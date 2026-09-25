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

## Интеграционные тесты

Тесты требуют настоящую PostgreSQL и роль с правами `CREATE DATABASE`. Каждый тест создаёт отдельную базу `ecobilling_test_<guid>` и удаляет её после выполнения.

```powershell
$env:ECOBILLING_TEST_POSTGRES_CONNECTION = '<test PostgreSQL connection string>'
dotnet test tests/EcoBilling.IntegrationTests/EcoBilling.IntegrationTests.csproj
```

Если переменная не задана, PostgreSQL-тесты явно помечаются как пропущенные; EF InMemory и SQLite не используются как замена.
