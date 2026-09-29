# Статус реализации EcoBilling

Состояние backend v1 после merge в `master` и production-readiness hardening. Базовый v1 verification pass был полностью зелёным: restore, Release build, architecture tests, unit tests, PostgreSQL integration tests, E2E, Docker Compose validation и container build.

## Реализованный функционал

| Область | Реализованный объём |
|---|---|
| Архитектура | Модульный монолит, отдельные composition roots API и Worker, PostgreSQL/EF Core/Npgsql, SharedKernel. |
| Identity | Email/account-number login, HS256 access JWT, refresh rotation/replay protection, lockout, initial password setup, role policies, auth rate limiting и Director-only revoke всех refresh sessions пользователя. |
| Director | Provisioning через внутренний RS256 service JWT и self-profile endpoint. |
| Controllers | Создание Director-ом, directory endpoints, профиль, назначения на Address, удаление назначения, список назначений, self assignments и worklist. |
| Residents | Создание и password reset Director-ом, directory endpoints, self-service profile/account/meters/readings/charges/payments/financial summary. |
| Accounts / Addresses | Account и Address lookup, Account overpayment как отдельное финансовое состояние v1. |
| Meters | Создание, получение, список по Account, вывод старого Meter из эксплуатации и создание replacement Meter с историей связи. |
| Readings | Автор/источник/время, resource-based доступ Controller, запрет уменьшения обычного показания, backdated правила, отдельные correction записи без перезаписи истории. |
| Tariffs | Создание Tariff, immutable TariffVersion, закрытие периода версии, история версий, назначение Tariff на Account, история и закрытие assignment. |
| Billing | Календарный месяц Asia/Bishkek, разница показаний × TariffVersion rate, KGS, AwayFromZero 2 decimals, сохранение входных readings/version/calculation version, защита от повторного начисления. |
| Meter replacement billing | Если период пересекает несколько Meter, расчёт явно блокируется до утверждения отдельного правила вместо скрытого предположения. |
| Payments | Ручной подтверждённый платеж Director-ом, обязательный Idempotency-Key, conflict detection по semantic request, распределение на старейшие Charges, PaymentAllocation и Account overpayment. |
| Overpayment | Остаток Payment хранится на Account и автоматически применяется к последующим Charges через PaymentAllocation. |
| Reports | Operational summary и financial summary: charges, payments, debt, overpayment, consumption и показатели Controller. |
| Audit | Mutation-сценарии записывают AuditLog атомарно с бизнес-изменением; AuditLog остаётся append-only. |
| Outbox | Billing/Payment создают OutboxMessage атомарно; dispatcher поддерживает retry state и distributed coordination. Внешний publisher не подменяется фиктивной доставкой и должен быть подключён после выбора transport/provider. |
| Worker | Monthly billing batch, Outbox task, retry runner, конфигурируемые schedules и PostgreSQL distributed lock между экземплярами Worker. |
| API | RFC 7807 Problem Details, correlation IDs, Swagger/OpenAPI только Development, role/resource policies и forwarded headers только от явно доверенных proxy IP. |

## Основная HTTP-поверхность

### Authentication
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/revoke`
- `POST /api/v1/auth/setup-password`

### Director / management
- `GET /api/v1/directors/me/profile`
- `GET|POST /api/v1/controllers`
- `GET /api/v1/controllers/{controllerId}`
- `GET|POST /api/v1/controllers/{controllerId}/assignments`
- `DELETE /api/v1/controllers/{controllerId}/assignments/{assignmentId}`
- `GET|POST /api/v1/residents`
- `GET /api/v1/residents/{residentId}`
- `PUT /api/v1/residents/{residentId}/password`
- `POST /api/v1/users/{userId}/sessions/revoke-all`
- Account/Address/Meter/Tariff/Billing/Payment/Report management endpoints.

### Controller
- `GET /api/v1/controllers/me/profile`
- `GET /api/v1/controllers/me/assignments`
- `GET /api/v1/controllers/me/worklist`
- `POST /api/v1/meters/{meterId}/readings` with assignment-based access check.

### Resident
- `GET /api/v1/me/profile`
- `GET /api/v1/me/account`
- `GET /api/v1/me/meters`
- `GET /api/v1/me/readings`
- `GET /api/v1/me/charges`
- `GET /api/v1/me/payments`
- `GET /api/v1/me/financial`

## Что намеренно не симулируется

Следующие возможности не могут быть качественно завершены кодом без отдельного утверждённого внешнего решения:

- платёжный provider callback/signature/refund/reconciliation;
- конкретный внешний Outbox transport/publisher;
- сложная billing formula v2+ (льготы, нормативы, пени и перерасчёты);
- неоднозначное начисление месяца, пересекающего замену Meter;
- production secret store и фактическое применение PostgreSQL migrator/runtime role model в целевом окружении;
- TLS/ingress/network policy конкретной площадки;
- backup/restore, RPO/RTO/SLA;
- telemetry backend, dashboards и alert thresholds;
- retention policy и тяжёлые report exports.

Для этих пунктов код не создаёт фиктивное успешное поведение.

## Verification status

Автоматизированный verification pass завершён успешно:

1. `dotnet restore` — успешно;
2. Release build — успешно;
3. EF Core migrations и ModelSnapshot синхронизированы;
4. Architecture tests — 34/34;
5. Unit tests — 307/307;
6. PostgreSQL integration tests — 206/206;
7. E2E journeys — 4/4;
8. Docker Compose validation — успешно;
9. container images `api`, `worker`, `migrations` — успешно;
10. GitHub Actions CI — зелёный.

Итого автоматизированных тестов: 551/551.

Backend v1 является проверенным staging candidate. Production GO требует environment-specific evidence из [production readiness checklist](operations/production-readiness.md): secrets, TLS/ingress/trusted proxies, PostgreSQL role split, backup/restore, observability, final image scanning и migration rehearsal. Внешний Outbox transport и payment provider требуются только если соответствующие сценарии включаются.
