# Матрица End-to-End покрытия

E2E-тесты выполняют пользовательский или межсервисный путь через настоящий ASP.NET Core HTTP pipeline и отдельную временную PostgreSQL. Эта матрица отличает наличие production-контракта от наличия отдельного E2E journey.

| Сценарий | Production-контракт | Автоматизированное покрытие |
|---|---|---|
| Вход Director | Реализован | E2E: provisioning → password setup → login. |
| Вход Controller | Реализован | E2E: создание → обязательная замена initial credential → login. |
| Вход Resident | Реализован | E2E: создание → login по Account number. |
| Неверный пароль / lockout | Реализован | Integration HTTP/PostgreSQL. |
| Запрет публичной регистрации | Реализован | E2E route inventory + RFC 7807 `404`. |
| Создание Resident | Реализован | E2E + integration: Identity/Profile/Address/Account/Audit/idempotency. |
| Сброс пароля Resident | Реализован | E2E + integration: revoke refresh sessions, старый пароль/refresh запрещены. |
| Создание Controller | Реализован | E2E + integration: audit/idempotency/password setup. |
| Resident видит только свои данные | Реализован | Ownership задаётся через JWT `sub` и self-service endpoints; integration/authorization tests. Отдельный multi-resident E2E journey ещё не выделен. |
| Controller assignments | Реализован | Unit/integration/API authorization; отдельный полный assignment E2E journey следует поддерживать как регрессию. |
| Controller worklist | Реализован | Unit/integration/read-model tests; endpoint доступен только Controller. |
| Controller вносит Reading только по назначенному Address | Реализован | Persistence/resource authorization tests; рекомендуется отдельный сквозной E2E journey. |
| История Readings | Реализована | Query/unit/integration; Resident self-service и Director management endpoints. |
| Tariff / TariffVersion / Account assignment | Реализован | Unit/integration/persistence/API tests. |
| Monthly Billing v1 | Реализован | Unit/integration с настоящей PostgreSQL; повтор периода идемпотентен. |
| Payment / allocation / overpayment | Реализован | Unit/integration с настоящей PostgreSQL. |
| Resident charges/payments/financial | Реализован | Self-service HTTP endpoints + integration/read-model tests. |
| Административный revoke всех refresh sessions | Реализован | Unit + HTTP authorization/route coverage; persistence audit выполняется атомарно. |
| Payment provider callback/refund/reconciliation | Не реализован | Ожидает выбор провайдера и контракт подписи. |
| Внешний Outbox transport | Не реализован | Persistence/dispatcher реализованы; publisher зависит от выбранного transport. |
| Billing периода через замену нескольких Meter | Намеренно блокируется | Возвращается явная ошибка до утверждения формулы. |
| Пени, льготы и сложные перерасчёты | Не реализованы | Ожидают версионируемую business policy v2+. |

## Запуск

```powershell
$env:ECOBILLING_TEST_POSTGRES_CONNECTION = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=<local-test-password>"
dotnet test tests/EcoBilling.EndToEndTests/EcoBilling.EndToEndTests.csproj --configuration Release
```

GitHub Actions задаёт административную строку только для одноразовой тестовой PostgreSQL. Production credentials для E2E не используются.

При добавлении нового пользовательского mutation/query сценария матрица должна обновляться вместе с тестами. Наличие строки «Production-контракт реализован» не должно автоматически интерпретироваться как наличие отдельного E2E journey.
