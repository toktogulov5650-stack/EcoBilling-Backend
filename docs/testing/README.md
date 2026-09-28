# Тестирование

- Unit — бизнес-правила без инфраструктуры.
- Unit также проверяет инфраструктурно-независимый runner Worker: успешное выполнение, ограниченные повторы, исчерпание попыток и отмену.
- Integration — база данных и интеграции.
- Contract/integration проверки внутреннего API запускают настоящий ASP.NET Core pipeline и PostgreSQL: RS256 JWT, обязательный `kid` и scope, одноразовый `jti`, RFC 7807 и идемпотентный повтор.
- User-auth integration-тесты проверяют настоящий HTTP pipeline и PostgreSQL: login, подписанный access JWT, hash-only refresh storage, rotation, replay-family revoke, logout, persisted lockout и замену начальной тайны.
- Controller-creation тесты проверяют role policy, RFC 7807, HMAC fingerprint, атомарную persistence, replay/conflict, конкурентный запрос и полный E2E: provisioning Director → login → создание Controller → password setup → login Controller.
- Resident-creation тесты проверяют Director policy, RFC 7807, HMAC fingerprint, атомарные Identity/Profile/Address/Account/Operation/Audit, replay/conflict, конкурентный запрос, ограничение один Account на Resident и полный E2E до входа Resident по лицевому счёту.
- Resident password-reset тесты проверяют Director policy, идемпотентность, снятие lockout, атомарный отзыв всех refresh-сессий, отсутствие credential в Audit и E2E: старый пароль/refresh запрещены, новый пароль работает.
- Integration-тесты Audit/Outbox проверяют миграцию на настоящей PostgreSQL, JSONB, UTC-время, append-only защиту аудита, переходы Outbox и отсутствие дубликата аудита при replay/concurrency.
- Integration-тесты observability проверяют независимость liveness от PostgreSQL, readiness failure без утечки строки подключения, correlation ID и валидацию OTLP endpoint.
- Unit-тесты Worker дополнительно проверяют activity и безопасные outcome tags фонового задания.
- Architecture — направления зависимостей.
- Architecture также проверяет Dockerfile/Compose: non-root runtime, health checks, обязательные секреты, правильный ключ connection string и порядок PostgreSQL → migrations → приложения.
- Architecture фиксирует CI-контракт: точный SDK, read-only GitHub permissions, Release build/test, PostgreSQL service и обязательную проверку Docker Compose.
- Architecture проверяет наличие обязательных руководств и разрешение всех локальных Markdown-ссылок.
- EndToEnd — сквозные сценарии через настоящий HTTP pipeline и отдельную временную PostgreSQL. Текущее и отложенное покрытие перечислено в [матрице E2E](end-to-end-coverage.md).

## Локальная проверка CI

Минимальная проверка без PostgreSQL:

```powershell
dotnet restore EcoBilling.slnx -warnaserror
dotnet package list --project EcoBilling.slnx --include-transitive --vulnerable --no-restore
dotnet build EcoBilling.slnx --configuration Release --no-restore
dotnet test EcoBilling.slnx --configuration Release --no-build
docker compose --env-file deploy/.env.example --file deploy/compose.yml config --quiet
```

Без `ECOBILLING_TEST_POSTGRES_CONNECTION` PostgreSQL-тесты помечаются как пропущенные. В GitHub Actions workflow поднимает временный `postgres:17-alpine`, задаёт эту переменную и выполняет все integration/contract tests.

`Directory.Build.props` включает NuGet audit для прямых и транзитивных пакетов с уровнем `low`. ArchitectureTests дополнительно фиксируют полный SHA сторонних GitHub Actions, запрет wildcard `AllowedHosts` и обязательность host allowlist в Compose.

Та же переменная включает E2E-тесты. Они применяют миграции к отдельной чистой базе, выполняют HTTP journey и удаляют базу после завершения.

## Полный локальный прогон с PostgreSQL

Ниже используется отдельный временный контейнер и host-порт `55432`. Значения предназначены только для изолированных локальных тестов.

```powershell
docker run --detach --rm --name ecobilling-tests-postgres `
  --env POSTGRES_PASSWORD=ecobilling_local_tests `
  --publish 127.0.0.1:55432:5432 `
  --health-cmd "pg_isready -U postgres -d postgres" `
  --health-interval 2s --health-timeout 3s --health-retries 30 `
  postgres:17-alpine

$env:ECOBILLING_TEST_POSTGRES_CONNECTION = "Host=127.0.0.1;Port=55432;Database=postgres;Username=postgres;Password=ecobilling_local_tests;Include Error Detail=false"
dotnet restore EcoBilling.slnx -warnaserror
dotnet build EcoBilling.slnx --configuration Release --no-restore
dotnet test EcoBilling.slnx --configuration Release --no-build

docker stop ecobilling-tests-postgres
Remove-Item Env:ECOBILLING_TEST_POSTGRES_CONNECTION
```

Пользователь подключения должен иметь права создавать и удалять базы. Каждый PostgreSQL integration/E2E-тест работает с отдельной базой, поэтому production endpoint и production credentials использовать нельзя. Контейнер запущен с `--rm` и удаляется после `docker stop`; если выполнение было прервано, остановите его вручную.

CI запускается для pull request и push в `main`, а release workflow повторяет тот же gate перед публикацией artifacts. Используемые в workflow пароли принадлежат только одноразовой тестовой PostgreSQL и не применяются при развёртывании.

Полный `dotnet format --verify-no-changes` пока не является CI-gate из-за исторически смешанных окончаний строк и кодировок в существующих файлах. Изменяемые C#-файлы форматируются адресно; нормализацию всего репозитория следует выполнять отдельным механическим изменением.
