# База данных

EcoBilling использует одну PostgreSQL на экземпляр округа и один `EcoBillingDbContext` модульного монолита.

## Identity

Первая миграция `InitialIdentityPersistence` создаёт схему `identity` и таблицу `identity.user_accounts`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `login_type` | `integer` | NOT NULL, значение 1 или 2 |
| `normalized_login` | `text` | NOT NULL |
| `password_hash` | `text` | NOT NULL |
| `role` | `integer` | NOT NULL, значение 1, 2 или 3 |
| `requires_password_change` | `boolean` | NOT NULL |
| `failed_login_attempts` | `integer` | NOT NULL, не меньше 0 |
| `lockout_end` | `timestamp with time zone` | NULL, UTC |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Уникальный индекс `ux_user_accounts_login_type_normalized_login` создан по `(login_type, normalized_login)`. Код округа и профильные данные пользователя в таблице отсутствуют.

Миграция `AddRefreshSessions` добавляет `identity.refresh_sessions`: `user_id`, `family_id`, уникальный SHA-256 `token_hash`, UTC-времена создания/истечения/использования/отзыва и ссылку на заменившую сессию. Raw refresh token в базе отсутствует. Повтор использованного token и явный revoke отзывают активные записи всей семьи одной транзакцией.

## Миграции

Локальный инструмент восстанавливается и запускается из корня репозитория:

```powershell
dotnet tool restore
$env:ECOBILLING_DESIGN_TIME_CONNECTION = '<development PostgreSQL connection string>'
dotnet ef database update --project src/EcoBilling.Infrastructure --startup-project src/EcoBilling.Infrastructure
```

Строка подключения не должна попадать в исходный код, `appsettings`, логи или отчёты. Миграции не редактируются вручную без отдельного объяснения.
Rollback первой миграции удаляет таблицу и созданную ею схему `identity`; повторное применение создаёт их заново.

В контейнерном контуре миграции выполняет отдельный одноразовый service до старта API и Worker. Runtime-приложения схему автоматически не меняют. Для production предусмотрены отдельные роли `ecobilling_migrator` и `ecobilling_runtime`; bootstrap/grant scripts находятся в `deploy/postgres` и должны быть применены и проверены в целевом окружении. Порядок обновления и ограничения rollback описаны в [эксплуатационном runbook](../operations/README.md), а переменные подключения — в [справочнике конфигурации](../configuration/README.md).

## Residents

Миграция `AddResidentsProfile` создаёт schema `residents` и таблицу `residents.residents`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `user_id` | `uuid` | NOT NULL, unique, FK на `identity.user_accounts.id` с `RESTRICT` |
| `full_name` | `text` | NOT NULL |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Rollback миграции удаляет таблицу и schema `residents`, не затрагивая Identity.

Миграция `AddResidentCreation` добавляет `residents.resident_creation_operations` с уникальными `idempotency_key`, `resident_id`, `account_id` и `address_id`, HMAC-SHA-256 fingerprint и UTC-временем. Операция ссылается на Resident, Account и Address через `RESTRICT`. Identity, Resident, Address, Account, operation и Audit создаются одной транзакцией.

Миграция `AddResidentPasswordReset` добавляет `residents.resident_password_reset_operations` с уникальным `idempotency_key`, HMAC-SHA-256 fingerprint, ссылкой на Resident и UTC-временем. Смена password hash, очистка lockout, отзыв refresh-сессий, operation и Audit выполняются одной транзакцией; открытый пароль и hash в operation/Audit отсутствуют.

## Controllers

Миграция `AddControllersProfile` создаёт schema `controllers` и таблицу `controllers.controllers`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `user_id` | `uuid` | NOT NULL, unique, FK на `identity.user_accounts.id` с `RESTRICT` |
| `full_name` | `text` | NOT NULL |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Rollback миграции удаляет таблицу и schema `controllers`, не затрагивая Identity или Residents.

Миграция `AddControllerCreation` добавляет `controllers.controller_creation_operations` с уникальными `idempotency_key` и `controller_id`, HMAC-SHA-256 request fingerprint и UTC-временем создания. Запись ссылается на `controllers.controllers` через `RESTRICT`. Она обеспечивает безопасный повтор Director-команды без хранения начальной тайны; создание `identity.user_accounts`, Controller, operation и Audit выполняется одной транзакцией.

## Accounts

Миграция `AddAccounts` создаёт schema `accounts` и первоначальную таблицу `accounts.accounts`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `resident_id` | `uuid` | NOT NULL, FK на `residents.residents.id` с `RESTRICT` |
| `account_number` | `text` | NOT NULL, unique, хранится в канонической форме |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Миграция `AddAddresses` позднее добавляет обязательный `address_id` и индекс `ix_accounts_address_id`. Миграция `AddResidentCreation` заменяет прежний индекс Resident на уникальный `ux_accounts_resident_id`, фиксируя правило v1 «один Resident — один Account». Финальная модель v1 также хранит `overpayment numeric(18,2)` с ограничением `overpayment >= 0`; задолженность вычисляется из Charges и PaymentAllocations.

Rollback миграции удаляет таблицу и schema `accounts`, не затрагивая Identity, Residents или Controllers.

## Addresses

Миграция `AddAddresses` создаёт таблицу `accounts.addresses` и добавляет обязательную связь `accounts.accounts.address_id`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `locality` | `text` | NOT NULL |
| `street` | `text` | NOT NULL |
| `house` | `text` | NOT NULL |
| `building` | `text` | nullable |
| `apartment` | `text` | nullable |
| `search_text` | `text` | NOT NULL, индекс `ix_addresses_search_text` |

Уникальность полного адреса и `accounts.address_id` не вводится. Удаление Address, на который ссылается Account, запрещено через `RESTRICT`.

Для существующих строк `accounts.accounts` нельзя безопасно угадать Address. Поэтому миграция сначала проверяет отсутствие таких строк и останавливается с явной ошибкой вместо создания фиктивного адреса. Перед production-применением к непустой базе требуется согласованный backfill.

Rollback `AddAddresses` удаляет внешний ключ и `address_id`, затем таблицу `accounts.addresses`; остальные данные Accounts сохраняются.

## Meters

Миграция `AddMeters` создаёт schema `meters` и таблицу `meters.meters`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `account_id` | `uuid` | NOT NULL, FK на `accounts.accounts.id` с `RESTRICT` |
| `serial_number` | `text` | NOT NULL, хранится в канонической форме |
| `installed_at` | `timestamp with time zone` | NOT NULL, UTC |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Неуникальный индекс `ix_meters_account_id` поддерживает получение счётчиков Account. Уникальные ограничения на `account_id` и `serial_number` не вводятся до утверждения кардинальности и области уникальности серийного номера.

Rollback миграции удаляет таблицу и schema `meters`, не затрагивая Accounts и Addresses.

## Readings

Миграция `AddReadings` создаёт schema `readings` и таблицу `readings.meter_readings`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `meter_id` | `uuid` | NOT NULL, FK на `meters.meters.id` с `RESTRICT` |
| `value` | `numeric` | NOT NULL, значение не меньше нуля |
| `measured_at` | `timestamp with time zone` | NOT NULL, UTC |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Неуникальный индекс `ix_meter_readings_meter_id_measured_at` создан по `(meter_id, measured_at)`. Precision и scale для `value`, уникальность времени измерения и проверка монотонности не вводятся до утверждения бизнес-правил.

Сгенерированный rollback дополнен удалением пустой schema `readings`, чтобы откат был симметричен применению миграции. Он не затрагивает Meters.

## Tariffs

Миграция `AddTariffs` создаёт schema `tariffs` и две таблицы.

`tariffs.tariffs`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `name` | `text` | NOT NULL |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

`tariffs.tariff_versions`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `tariff_id` | `uuid` | NOT NULL, FK на `tariffs.tariffs.id` с `RESTRICT` |
| `rate` | `numeric` | NOT NULL, значение не меньше нуля |
| `effective_from` | `date` | NOT NULL |
| `effective_to` | `date` | nullable, позже `effective_from` |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Неуникальный индекс `ix_tariff_versions_tariff_id_effective_from` поддерживает чтение истории. Exclusion constraint `ex_tariff_versions_tariff_id_effective_period` использует `daterange(..., '[)')` и расширение `btree_gist`, чтобы периоды одного Tariff не пересекались на уровне базы данных.

Precision и scale ставки не фиксируются до утверждения валюты и единицы измерения. Rollback удаляет обе таблицы и schema `tariffs`, не затрагивая Readings. Расширение `btree_gist` сохраняется как глобальный объект базы данных: оно могло быть установлено администратором или использоваться другими схемами.

## Billing

Миграция `AddBilling` создаёт schema `billing` и таблицу `billing.charges`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `account_id` | `uuid` | NOT NULL, FK на `accounts.accounts.id` с `RESTRICT` |
| `tariff_version_id` | `uuid` | NOT NULL, FK на `tariffs.tariff_versions.id` с `RESTRICT` |
| `period_start` | `date` | NOT NULL |
| `period_end` | `date` | NOT NULL, позже `period_start` |
| `amount` | `numeric` | NOT NULL, знак и точность не ограничены |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Уникальный индекс `ux_charges_account_id_period_start_period_end` предотвращает точный повтор начисления одного периода для Account. Индекс `ix_charges_tariff_version_id` поддерживает связь с исторической версией тарифа.

Миграция не создаёт формулу, статус или баланс. Rollback удаляет таблицу и schema `billing`, не затрагивая Accounts и Tariffs.

## Payments

Миграция `AddPayments` создаёт schema `payments` и таблицу `payments.payments`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `account_id` | `uuid` | NOT NULL, FK на `accounts.accounts.id` с `RESTRICT` |
| `amount` | `numeric` | NOT NULL, значение больше нуля |
| `idempotency_key` | `text` | NOT NULL, unique во всём экземпляре округа |
| `paid_at` | `timestamp with time zone` | NOT NULL, UTC |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Индекс `ix_payments_account_id` поддерживает получение истории Account. Уникальный индекс `ux_payments_idempotency_key` обеспечивает базовую идемпотентность; сравнение ключей регистрозависимо, значение сохраняется без нормализации.

Миграция не создаёт ProviderReference, callback, статус, распределение по начислениям или баланс. Rollback удаляет таблицу и schema `payments`, не затрагивая Accounts или Billing.

## Reports

Reports не создаёт отдельную schema, таблицу или миграцию. `DistrictOperationalSummary` выполняет один read-only PostgreSQL statement с `count(*)` по существующим таблицам модулей и не создаёт вторичный источник истины.

Финансовые суммы, задолженность и показатели работы контроллеров не вычисляются до утверждения соответствующих правил. Для тяжёлых отчётов в будущем должен использоваться Worker, а не длительный синхронный HTTP-запрос.

## Audit и Outbox

Миграция AddAuditAndOutbox добавляет две технические таблицы в schema infrastructure.

infrastructure.audit_logs:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| id | uuid | primary key |
| actor_type | varchar(64) | NOT NULL |
| actor_id | varchar(200) | NOT NULL |
| action | varchar(200) | NOT NULL |
| entity_type | varchar(200) | NOT NULL |
| entity_id | varchar(200) | NOT NULL |
| before_data | jsonb | nullable |
| after_data | jsonb | nullable |
| correlation_id | varchar(200) | NOT NULL |
| created_at | timestamp with time zone | NOT NULL, UTC |

EcoBillingDbContext отклоняет tracked update/delete AuditLog. Production-роль базы
дополнительно должна получить только необходимые права; срок хранения аудита пока не
утверждён.

infrastructure.outbox_messages:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| id | uuid | primary key |
| type | varchar(200) | NOT NULL |
| payload | jsonb | NOT NULL |
| occurred_at | timestamp with time zone | NOT NULL, UTC |
| processed_at | timestamp with time zone | nullable |
| retry_count | integer | NOT NULL, не меньше нуля |
| last_error | varchar(2000) | nullable |

Индекс (processed_at, occurred_at) поддерживает выборку ожидающих сообщений. Реальные
producer, publisher, lease/locking, retry/backoff и dead-letter не фиксируются до появления
утверждённой внешней интеграции. Provisioning директора Outbox-сообщение не создаёт.

## Интеграционные тесты

Тесты требуют настоящую PostgreSQL и роль с правами `CREATE DATABASE`. Каждый тест создаёт отдельную базу `ecobilling_test_<guid>` и удаляет её после выполнения.

```powershell
$env:ECOBILLING_TEST_POSTGRES_CONNECTION = '<test PostgreSQL connection string>'
dotnet test tests/EcoBilling.IntegrationTests/EcoBilling.IntegrationTests.csproj
```

Если переменная не задана, PostgreSQL-тесты явно помечаются как пропущенные; EF InMemory и SQLite не используются как замена.
