# Статус реализации EcoBilling

Состояние ветки `feature/complete-v1-backend` после завершения основной функциональной реализации v1. Этот документ описывает фактически написанный код; финальная подтверждённая готовность определяется только после полного build/test/migration/CI прогона.

## Реализованный функционал

| Область | Реализованный объём |
|---|---|
| Архитектура | Модульный монолит, отдельные composition roots API и Worker, PostgreSQL/EF Core/Npgsql, SharedKernel. |
| Identity | Email/account-number login, HS256 access JWT, refresh rotation/replay protection, lockout, initial password setup, role policies и rate limiting authentication endpoints. |
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
| API | RFC 7807 Problem Details, correlation IDs, Swagger/OpenAPI только Development, role policies и resource checks. |

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
- production secret store и PostgreSQL role model;
- TLS/ingress/network policy конкретной площадки;
- backup/restore, RPO/RTO/SLA;
- telemetry backend, dashboards и alert thresholds;
- retention policy и тяжёлые report exports.

Для этих пунктов код не создаёт фиктивное успешное поведение.

## Следующий этап проверки

После завершения source-кода выполняется отдельный verification pass:

1. restore и Release build;
2. исправление compile/warnings-as-errors;
3. генерация финальной EF migration и ModelSnapshot;
4. unit tests;
5. PostgreSQL integration tests;
6. architecture tests;
7. E2E journeys;
8. migration fresh-db/upgrade checks;
9. Docker Compose / Worker / health checks;
10. CI и security regression.

До успешного завершения этого прогона ветка не считается production-ready.
