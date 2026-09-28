# Статус реализации EcoBilling

Состояние зафиксировано на 28 сентября 2026 года после финальной проверки безопасности и готовности. Этот документ отделяет работающие контракты от архитектурной основы для будущих функций.

## Реализовано

| Область | Подтверждённый объём |
|---|---|
| Архитектура | Модульный монолит с проверяемыми направлениями `ProjectReference` и отдельными composition roots API и Worker. |
| Identity | Вход по email/лицевому счёту, HS256 access JWT с ротацией `kid`, одноразовые hashed refresh sessions, token-family revoke/replay detection, persisted lockout и первичная установка пароля сотрудников. |
| Director provisioning | Защищённый `POST /internal/v1/directors`, RS256 service JWT, защита `jti` от повтора, идемпотентность, атомарное создание и аудит. |
| Controllers | Director-only `POST /api/v1/controllers`: JWT policy, обязательная идемпотентность, атомарные Identity/Profile/Operation/Audit, начальная тайна и полный E2E до первого входа Controller. |
| Residents | Director-only создание Resident и сброс пароля: атомарные Identity/Profile/Address/Account/Operation/Audit, идемпотентность, отзыв refresh-сессий и полный E2E входа/восстановления доступа. |
| Accounts и Addresses | Доменные модели, обязательные связи, нормализация, один Account на Resident в v1 и PostgreSQL constraints без финансового Balance. |
| Meters и Readings | Минимальные доменные и persistence-модели без неподтверждённого жизненного цикла и mutation-сценариев. |
| Tariffs | Tariff и неизменяемые непересекающиеся версии ставок без административного API. |
| Billing | Минимальная запись Charge и защита точного дубля периода без формулы расчёта. |
| Payments | Подтверждённый Payment с уникальным idempotency key без провайдерского callback и распределения оплаты. |
| Reports | Read-only операционная сводка количества сущностей без финансовых расчётов и HTTP endpoint. |
| Audit и Outbox | Append-only AuditLog для provisioning и persistence-основа Outbox без producer/dispatcher. |
| Worker | Общий runner с cancellation, ограниченными retry, логами, traces и metrics; реальные задания не зарегистрированы. |
| Эксплуатация | JSON-логи, OpenTelemetry, liveness/readiness, Docker Compose, отдельный migration job и CI с PostgreSQL. |
| E2E | Provisioning Director, создание и вход Controller/Resident, идемпотентность и отсутствие публичной регистрации проверяются через реальный HTTP pipeline и PostgreSQL. |

## Текущая HTTP-поверхность

Production API предоставляет только:

- `GET /health/live`;
- `GET /health/ready`;
- `GET /health`;
- `POST /internal/v1/directors`;
- `POST /api/v1/auth/login`;
- `POST /api/v1/auth/refresh`;
- `POST /api/v1/auth/revoke`;
- `POST /api/v1/auth/setup-password`;
- `POST /api/v1/controllers`.
- `POST /api/v1/residents`.
- `PUT /api/v1/residents/{residentId}/password`.

OpenAPI публикуется только в `Development`. Полный контракт описан в [документации API](api/README.md).

## Не реализовано

- публичная регистрация — запрещена архитектурой;
- восстановление credentials Controller/Director и немедленный deny-list уже выпущенных access JWT;
- изменение профилей Resident/Controller;
- назначения контроллеров и resource-based доступ к жителям;
- пользовательские endpoints профилей, счетов, счётчиков, показаний, тарифов, начислений и платежей;
- жизненный цикл и замена счётчика;
- внесение и исправление показаний;
- административное управление тарифами;
- формула начисления, перерасчёт и финансовое состояние Account;
- платёжный провайдер, подпись callback, частичная оплата, возвраты и сверка;
- финансовые отчёты и экспорт;
- реальные Worker jobs и Outbox dispatcher;
- production backup/restore, RPO/RTO/SLA, alert thresholds и политика хранения данных.

Причины ожидания пользовательских E2E-сценариев перечислены в [матрице покрытия](testing/end-to-end-coverage.md). Неподтверждённые решения находятся в [открытом реестре](architecture/open-decisions.md).

## Готовность

Сборка, автоматические тесты, миграции чистой базы и контейнерный контур являются проверяемой технической основой. Финальная проверка подтвердила audit NuGet, SHA-pinning CI, host allowlist и безопасное логирование ошибок Worker, но production-решение остаётся **NO-GO** до закрытия блокеров из [отчёта безопасности](security/README.md) и обязательных решений реестра.
