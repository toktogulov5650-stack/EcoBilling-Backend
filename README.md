# EcoBilling

EcoBilling — backend системы учёта, начислений и оплаты воды для одного округа. Каждый округ разворачивает отдельные экземпляры API, Worker и PostgreSQL.

Центральная маршрутизация по коду округа, системные администраторы и реестр округов относятся к отдельному проекту `ecobilling-control` и не входят в этот репозиторий.

## Текущий статус

Основная функциональность backend v1 реализована и прошла автоматизированный verification pass: Release build, unit, architecture, PostgreSQL integration, E2E, Docker Compose validation и сборка container images.

Реализованы:

- Identity/Auth: login, access/refresh JWT, rotation/replay protection, lockout, password setup и rate limiting;
- Director provisioning и self-profile;
- создание Controller и Resident директором;
- Controller assignments по Address и controller worklist;
- Meter create/get/list/replace;
- Reading create/history/corrections/backdated rules и assignment-based access;
- Tariff, TariffVersion, назначение тарифа Account и закрытие периодов;
- Billing v1 для календарного месяца `Asia/Bishkek`;
- ручная регистрация подтверждённых Payments, PaymentAllocation и Account overpayment;
- Resident self-service;
- operational/financial reports;
- append-only AuditLog;
- Outbox persistence/dispatcher;
- Monthly Billing и Outbox Worker с PostgreSQL distributed locking;
- административный отзыв всех refresh-сессий пользователя;
- безопасная поддержка reverse proxy через явный allowlist доверенных proxy.

Проект является **staging-ready backend candidate**. Repository-side production hardening завершён и подтверждён зелёным CI; фактический production deployment остаётся **NO-GO**, пока не закрыты environment-specific блокеры: production secret store, применение раздельных PostgreSQL migration/runtime ролей, TLS/ingress/network policy, backup/restore с RPO/RTO, telemetry backend/dashboards/alerts, immutable release digest/SBOM и необходимые внешние интеграции.

Внешний payment provider и внешний Outbox transport намеренно не симулируются.

Подробный статус: [docs/project-status.md](docs/project-status.md). Production blockers: [docs/security/README.md](docs/security/README.md).

## Проекты и зависимости

- `EcoBilling.Api` — HTTP composition root, middleware, auth и endpoints.
- `EcoBilling.Modules` — бизнес-модули, вертикальные сценарии и контракты.
- `EcoBilling.Infrastructure` — PostgreSQL, EF Core, безопасность, audit, outbox и технические adapters.
- `EcoBilling.Worker` — фоновые задания без копирования бизнес-правил.
- `EcoBilling.SharedKernel` — небольшие общие технические типы.

```text
Api ─────┬─→ Infrastructure ─→ Modules ─→ SharedKernel
         └─→ Modules

Worker ──┬─→ Infrastructure
         └─→ Modules
```

Направления `ProjectReference` контролируются ArchitectureTests.

## Требования

- .NET SDK `10.0.401`, закреплённый в `global.json`;
- Docker Engine или Docker Desktop с Docker Compose v2;
- PostgreSQL 17 для полного integration/E2E прогона;
- PowerShell для примеров команд ниже.

## Быстрый локальный запуск

```powershell
Copy-Item deploy/.env.example deploy/.env
notepad deploy/.env
docker compose --env-file deploy/.env --file deploy/compose.yml config --quiet
docker compose --env-file deploy/.env --file deploy/compose.yml up --build --detach
docker compose --env-file deploy/.env --file deploy/compose.yml ps
Invoke-RestMethod http://localhost:8080/health/ready
```

`deploy/.env.example` предназначен только для локального/изолированного запуска. Production secrets должны приходить из внешнего secret store.

Остановка сохраняет named volume PostgreSQL:

```powershell
docker compose --env-file deploy/.env --file deploy/compose.yml down
```

## Сборка и тесты

```powershell
dotnet restore EcoBilling.slnx -warnaserror
dotnet build EcoBilling.slnx --configuration Release --no-restore
dotnet test EcoBilling.slnx --configuration Release --no-build
```

Без `ECOBILLING_TEST_POSTGRES_CONNECTION` тесты, требующие PostgreSQL, пропускаются. Полный локальный PostgreSQL-прогон описан в [docs/testing/README.md](docs/testing/README.md).

Последний полный verification pass production-readiness ветки:

- Architecture: 34/34;
- Unit: 307/307;
- PostgreSQL integration: 206/206;
- E2E: 4/4;
- Docker Compose validation: passed;
- images `api`, `worker`, `migrations`, `postgres`: built successfully;
- Trivy CRITICAL/HIGH scan: 0 findings для всех четырёх финальных images.

Production-readiness изменения подтверждены повторным зелёным CI.

## Основная HTTP-поверхность

### Identity
- `POST /api/v1/auth/login`
- `POST /api/v1/auth/refresh`
- `POST /api/v1/auth/revoke`
- `POST /api/v1/auth/setup-password`
- `POST /api/v1/users/{userId}/sessions/revoke-all` — Director-only administrative revoke.

### Director / management
- Director profile;
- Controller/Resident directories и создание;
- Controller assignments;
- Account/Address/Meter/Tariff/Billing/Payment/Report management endpoints.

### Controller
- self-profile;
- assignments;
- worklist;
- внесение показаний только по назначенным ресурсам.

### Resident
- profile;
- account/address;
- meters/readings;
- charges/payments;
- financial summary.

Точные маршруты и контракты: [docs/api/README.md](docs/api/README.md).

## Worker и внешние интеграции

Worker содержит реальные задачи monthly billing и outbox dispatch, но schedules по умолчанию выключены. Включать их следует только после проверки соответствующей среды и внешних зависимостей.

Не подключены намеренно:

- payment provider callback/signature/refund/reconciliation;
- конкретный внешний Outbox publisher/transport;
- billing v2+ (пени, льготы, нормативы, сложные перерасчёты);
- формула для billing-периода, пересекающего замену Meter.

## Production readiness

Перед production обязательны:

- внешний secret store и процедуры rotation/revocation;
- разные PostgreSQL credentials для schema migrations и runtime;
- TLS termination, trusted proxies и network restrictions;
- backup/restore drill с утверждёнными RPO/RTO;
- OTLP collector/backend, dashboards и alerts;
- фиксация immutable release digest/SBOM и supply-chain policy;
- migration rehearsal на реалистичной копии данных;
- rollback/forward-fix runbook;
- безопасный процесс передачи initial credentials сотрудникам.

См. [deployment](docs/deployment/README.md), [configuration](docs/configuration/README.md), [operations](docs/operations/README.md) и [security readiness](docs/security/README.md).

## Документация

Начальная точка: [docs/README.md](docs/README.md).
