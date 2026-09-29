# EcoBilling API v1

Этот документ описывает HTTP-поверхность, реализованную в ветке `feature/complete-v1-backend`. Финальная схема ответов подтверждается OpenAPI и E2E после verification pass.

## Общие правила

- Публичной регистрации нет.
- Director и Controller входят по email, Resident — по Account number.
- Ошибки возвращаются как RFC 7807 `application/problem+json` с `code` и `traceId`.
- Клиент может передавать `X-Correlation-Id`.
- Mutation-сценарии, где это требуется, используют идемпотентность и AuditLog.
- Authentication endpoints защищены rate limiting.
- OpenAPI/Swagger публикуется только в Development.

## Authentication

| Метод | Путь | Доступ |
|---|---|---|
| POST | `/api/v1/auth/login` | Anonymous |
| POST | `/api/v1/auth/refresh` | Anonymous |
| POST | `/api/v1/auth/revoke` | Anonymous |
| POST | `/api/v1/auth/setup-password` | Anonymous |

Access token действует по конфигурации (v1 default 15 минут), refresh token — 30 дней. Refresh token хранится как hash, одноразово ротируется; replay отзывает token family.

## Internal provisioning

| Метод | Путь | Доступ |
|---|---|---|
| POST | `/internal/v1/directors` | RS256 service JWT |

Provisioning использует отдельный issuer/audience/scope, `kid`, `jti` replay protection и `Idempotency-Key`.

## Directors

| Метод | Путь |
|---|---|
| GET | `/api/v1/directors/me/profile` |

## Controllers

| Метод | Путь | Доступ |
|---|---|---|
| GET | `/api/v1/controllers` | Director |
| GET | `/api/v1/controllers/{controllerId}` | Director |
| POST | `/api/v1/controllers` | Director |
| GET | `/api/v1/controllers/{controllerId}/assignments` | Director |
| POST | `/api/v1/controllers/{controllerId}/assignments` | Director |
| DELETE | `/api/v1/controllers/{controllerId}/assignments/{assignmentId}` | Director |
| GET | `/api/v1/controllers/me/profile` | Controller |
| GET | `/api/v1/controllers/me/assignments` | Controller |
| GET | `/api/v1/controllers/me/worklist` | Controller |

Controller может вводить показания только для Meter, чей Account связан с назначенным Controller Address.

## Residents

| Метод | Путь | Доступ |
|---|---|---|
| GET | `/api/v1/residents` | Director |
| GET | `/api/v1/residents/{residentId}` | Director |
| POST | `/api/v1/residents` | Director |
| PUT | `/api/v1/residents/{residentId}/password` | Director |
| GET | `/api/v1/me/profile` | Resident |
| GET | `/api/v1/me/account` | Resident |
| GET | `/api/v1/me/meters` | Resident |
| GET | `/api/v1/me/readings` | Resident |
| GET | `/api/v1/me/charges` | Resident |
| GET | `/api/v1/me/payments` | Resident |
| GET | `/api/v1/me/financial` | Resident |

Resident self-service никогда не принимает чужой `residentId`/ `accountId`: ownership определяется по `sub` access token.

## Accounts and Addresses

| Метод | Путь | Доступ |
|---|---|---|
| GET | `/api/v1/accounts/by-number/{accountNumber}` | Director |
| GET | `/api/v1/addresses/{addressId}` | Director |

## Meters

| Метод | Путь | Доступ |
|---|---|---|
| GET | `/api/v1/meters/{meterId}` | Director |
| GET | `/api/v1/accounts/{accountId}/meters` | Director |
| POST | `/api/v1/accounts/{accountId}/meters` | Director |
| POST | `/api/v1/meters/{meterId}/replace` | Director |

Replacement сохраняет старый Meter как retired и создаёт новый Meter со ссылкой `ReplacesMeterId`; история не переписывается.

## Readings

| Метод | Путь | Доступ |
|---|---|---|
| GET | `/api/v1/readings/{readingId}` | Director |
| GET | `/api/v1/meters/{meterId}/readings` | Director |
| POST | `/api/v1/meters/{meterId}/readings` | Director или назначенный Controller |

Обычное показание не может уменьшаться. Backdated ввод Controller запрещён. Director может выполнять backdated/correction операции только с причиной. Correction создаёт новую запись и ссылку на superseded reading; исходная история остаётся неизменной.

## Tariffs

| Метод | Путь | Доступ |
|---|---|---|
| GET | `/api/v1/tariffs` | Director |
| POST | `/api/v1/tariffs` | Director |
| GET | `/api/v1/tariffs/{tariffId}` | Director |
| GET | `/api/v1/tariffs/{tariffId}/versions` | Director |
| POST | `/api/v1/tariffs/{tariffId}/versions` | Director |
| GET | `/api/v1/tariffs/{tariffId}/versions/{versionId}` | Director |
| PUT | `/api/v1/tariffs/{tariffId}/versions/{versionId}/end` | Director |
| GET | `/api/v1/accounts/{accountId}/tariff` | Director |
| GET | `/api/v1/accounts/{accountId}/tariff-assignments` | Director |
| POST | `/api/v1/accounts/{accountId}/tariff-assignments` | Director |
| PUT | `/api/v1/accounts/{accountId}/tariff-assignments/{assignmentId}/end` | Director |

TariffVersion и Account assignment используют полуоткрытые периоды и не допускают overlap через mutation service.

## Billing

| Метод | Путь | Доступ |
|---|---|---|
| GET | `/api/v1/charges/{chargeId}` | Director |
| GET | `/api/v1/accounts/{accountId}/charges` | Director |
| POST | `/api/v1/accounts/{accountId}/billing/{year}/{month}` | Director |

v1: календарный месяц `Asia/Bishkek`, consumption = current accepted reading - previous accepted reading, amount = consumption × effective tariff rate, KGS, округление `AwayFromZero` до 2 знаков. Charge сохраняет reading IDs, TariffVersion и calculation version.

Повтор того же Account/month возвращает существующий Charge. Если расчётный месяц пересекает replacement нескольких Meter, API возвращает явную ошибку вместо неподтверждённой формулы.

## Payments

| Метод | Путь | Доступ |
|---|---|---|
| GET | `/api/v1/payments/{paymentId}` | Director |
| GET | `/api/v1/accounts/{accountId}/payments` | Director |
| GET | `/api/v1/accounts/{accountId}/financial` | Director |
| POST | `/api/v1/accounts/{accountId}/payments` | Director |

Manual payment требует `Idempotency-Key`. Оплата распределяется на старейшие непогашенные Charges через `PaymentAllocation`; остаток записывается в `Account.Overpayment` и применяется к последующим Charges.

Внешний payment provider/callback/refund не симулируется до выбора провайдера.

## Reports

| Метод | Путь | Доступ |
|---|---|---|
| GET | `/api/v1/reports/operational-summary` | Director |
| GET | `/api/v1/reports/financial-summary` | Director |

Financial summary включает charges, payments, outstanding debt, overpayment, consumption и показатели Controller.

## Health

- `GET /health/live`
- `GET /health/ready`
- `GET /health`

Readiness включает PostgreSQL, liveness не зависит от базы данных.
