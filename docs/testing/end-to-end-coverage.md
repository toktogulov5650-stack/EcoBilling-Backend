# Матрица End-to-End покрытия

E2E-тесты EcoBilling выполняют реальные HTTP journeys через ASP.NET Core pipeline и отдельную временную PostgreSQL.

Важно: наличие production endpoint не означает наличие отдельного E2E journey. Unit/integration/contract coverage и E2E coverage ниже различаются явно.

## Текущие E2E journeys

В проекте сейчас четыре journey-файла:

- `DirectorProvisioningJourneyTests`;
- `ControllerCreationJourneyTests`;
- `ResidentCreationJourneyTests`;
- `PublicHttpBoundaryJourneyTests`.

| Сценарий | Реализация API | Текущее покрытие |
|---|---|---|
| Provisioning первого Director | Реализовано | E2E |
| Login Director | Реализовано | E2E внутри Director journey |
| Создание Controller Director-ом | Реализовано | E2E |
| Password setup и login Controller | Реализовано | E2E |
| Создание Resident + Address + Account | Реализовано | E2E |
| Login Resident по Account number | Реализовано | E2E |
| Сброс пароля Resident | Реализовано | E2E внутри Resident journey |
| Запрет публичной регистрации | Реализовано как отсутствие маршрута | E2E public boundary |
| Полный inventory публичных routes | Реализовано | E2E public boundary |
| Controller assignments | Реализовано | Unit/integration; отдельный E2E journey ещё нужен |
| Controller worklist | Реализовано | Unit/integration; отдельный E2E journey ещё нужен |
| Controller не видит неназначенный ресурс | Реализовано resource-based authorization | Integration; отдельный E2E journey ещё нужен |
| Внесение Reading Controller-ом | Реализовано | Unit/integration; отдельный E2E journey ещё нужен |
| Reading correction/backdated rules | Реализовано | Unit/integration; отдельный E2E journey ещё нужен |
| Meter create/replace | Реализовано | Unit/integration; отдельный E2E journey ещё нужен |
| Tariff + TariffVersion + Account assignment | Реализовано | Unit/integration; отдельный E2E journey ещё нужен |
| Monthly Billing v1 | Реализовано | Unit/integration; отдельный E2E journey ещё нужен |
| Manual Payment + allocation + overpayment | Реализовано | Unit/integration; отдельный E2E journey ещё нужен |
| Resident self-service: meters/readings/charges/payments | Реализовано | Integration/API tests; отдельный E2E journey ещё нужен |
| Administrative revoke-all refresh sessions | Реализовано | Unit/integration; отдельный HTTP E2E journey ещё нужен |
| Payment provider callback/refund/reconciliation | Не реализовано | Ожидает выбор провайдера |
| Внешний Outbox transport | Не реализовано | Ожидает выбор transport/provider |
| Billing периода с заменой Meter | Явно блокируется | Ожидает утверждённое бизнес-правило |

## Рекомендуемые следующие E2E journeys

Перед production полезно добавить четыре сквозных сценария:

1. `ControllerAssignmentAndReadingJourney`: Director назначает Address → Controller видит worklist → вносит Reading → неназначенный Controller получает запрет.
2. `TariffAndBillingJourney`: Director создаёт Tariff/Version/assignment → показания → monthly charge → повтор расчёта не создаёт дубль.
3. `PaymentAndOverpaymentJourney`: Charge → частичная/полная оплата → allocation → overpayment → следующий Charge использует переплату.
4. `ResidentSelfServiceJourney`: после Billing/Payment Resident видит только собственные meters/readings/charges/payments/financial summary.

## Запуск

```powershell
$env:ECOBILLING_TEST_POSTGRES_CONNECTION = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=<local-test-password>"
dotnet test tests/EcoBilling.EndToEndTests/EcoBilling.EndToEndTests.csproj --configuration Release
```

GitHub Actions использует одноразовый PostgreSQL service. Production credentials для E2E не используются.
