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
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Уникальный индекс `ux_user_accounts_login_type_normalized_login` создан по `(login_type, normalized_login)`. Код округа и профильные данные пользователя в таблице отсутствуют.

## Миграции

Локальный инструмент восстанавливается и запускается из корня репозитория:

```powershell
dotnet tool restore
$env:ECOBILLING_DESIGN_TIME_CONNECTION = '<development PostgreSQL connection string>'
dotnet ef database update --project src/EcoBilling.Infrastructure --startup-project src/EcoBilling.Infrastructure
```

Строка подключения не должна попадать в исходный код, `appsettings`, логи или отчёты. Миграции не редактируются вручную без отдельного объяснения.
Rollback первой миграции удаляет таблицу и созданную ею схему `identity`; повторное применение создаёт их заново.

## Residents

Миграция `AddResidentsProfile` создаёт schema `residents` и таблицу `residents.residents`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `user_id` | `uuid` | NOT NULL, unique, FK на `identity.user_accounts.id` с `RESTRICT` |
| `full_name` | `text` | NOT NULL |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Rollback миграции удаляет таблицу и schema `residents`, не затрагивая Identity.

## Controllers

Миграция `AddControllersProfile` создаёт schema `controllers` и таблицу `controllers.controllers`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `user_id` | `uuid` | NOT NULL, unique, FK на `identity.user_accounts.id` с `RESTRICT` |
| `full_name` | `text` | NOT NULL |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Rollback миграции удаляет таблицу и schema `controllers`, не затрагивая Identity или Residents.

## Accounts

Миграция `AddAccounts` создаёт schema `accounts` и первоначальную таблицу `accounts.accounts`:

| Столбец | PostgreSQL | Ограничение |
|---|---|---|
| `id` | `uuid` | primary key |
| `resident_id` | `uuid` | NOT NULL, FK на `residents.residents.id` с `RESTRICT` |
| `account_number` | `text` | NOT NULL, unique, хранится в канонической форме |
| `created_at` | `timestamp with time zone` | NOT NULL, UTC |

Миграция `AddAddresses` позднее добавляет обязательный `address_id` и индекс `ix_accounts_address_id`. Неуникальные индексы связей не фиксируют неподтверждённую кардинальность. Поля Balance, долга и переплаты отсутствуют до утверждения финансовых правил.

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

## Интеграционные тесты

Тесты требуют настоящую PostgreSQL и роль с правами `CREATE DATABASE`. Каждый тест создаёт отдельную базу `ecobilling_test_<guid>` и удаляет её после выполнения.

```powershell
$env:ECOBILLING_TEST_POSTGRES_CONNECTION = '<test PostgreSQL connection string>'
dotnet test tests/EcoBilling.IntegrationTests/EcoBilling.IntegrationTests.csproj
```

Если переменная не задана, PostgreSQL-тесты явно помечаются как пропущенные; EF InMemory и SQLite не используются как замена.
