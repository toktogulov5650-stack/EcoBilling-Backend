# EcoBilling

EcoBilling — backend системы учёта и оплаты воды для одного округа. Каждый округ разворачивает отдельные экземпляры API, Worker и PostgreSQL.

Центральная маршрутизация по коду округа, системные администраторы и реестр округов относятся к отдельному проекту `ecobilling-control` и не входят в этот репозиторий.

## Текущий статус

В репозитории реализован backend v1 модульного монолита:

- Identity: Director / Controller / Resident, login, password setup, refresh rotation, replay protection, lockout и отзыв сессий;
- Director provisioning через отдельный RS256 service JWT;
- создание и directory-сценарии Controller и Resident;
- Controller assignments на Address и Controller worklist;
- Account / Address lookup;
- Meter create/list/get/replace с сохранением истории;
- MeterReading create/list/get, resource-based доступ Controller, backdated/correction правила;
- Tariff, TariffVersion и назначения тарифа на Account с периодами действия;
- месячный Billing v1;
- ручная регистрация подтверждённых Payments, PaymentAllocation и Account overpayment;
- Resident self-service;
- operational и financial reports;
- append-only AuditLog, Outbox persistence/dispatcher;
- Monthly Billing Worker и Outbox Worker с PostgreSQL distributed lock;
- structured logging, OpenTelemetry, liveness/readiness;
- Docker Compose, EF Core migrations и CI с настоящей PostgreSQL.

Публичной регистрации нет. Не реализуются фиктивно внешние возможности, для которых требуется отдельное решение: payment provider callbacks/refunds/reconciliation, внешний Outbox transport, льготы/пени/сложные перерасчёты и формула начисления периода, пересекающего замену нескольких Meter.

Production readiness зависит не только от backend-кода. Перед production необходимо закрыть environment-specific задачи: secret store, TLS/ingress, доверенные proxies, отдельные PostgreSQL credentials для migrations/runtime, backup/restore с RPO/RTO, telemetry backend/alerts и container vulnerability policy. Подробности находятся в [статусе проекта](docs/project-status.md) и [production readiness checklist](docs/operations/production-readiness.md).

## Проекты и зависимости

- `EcoBilling.Api` — HTTP composition root, middleware, аутентификация и endpoints.
- `EcoBilling.Modules` — бизнес-модули, сценарии и контракты.
- `EcoBilling.Infrastructure` — PostgreSQL, безопасность, аудит и технические адаптеры.
- `EcoBilling.Worker` — composition root фонового выполнения без копирования бизнес-правил.
- `EcoBilling.SharedKernel` — небольшие общие технические типы.

```text
Api ─────┬─→ Infrastructure ─→ Modules ─→ SharedKernel
         └─→ Modules

Worker ──┬─→ Infrastructure
         └─→ Modules
```

Направления `ProjectReference` контролируются исполняемыми ArchitectureTests.

## Требования

- .NET SDK `10.0.401`, закреплённый в `global.json`;
- Docker Engine или Docker Desktop с Docker Compose v2;
- PostgreSQL для полного integration/E2E прогона.

## Быстрый запуск через Docker Compose

```powershell
Copy-Item deploy/.env.example deploy/.env
notepad deploy/.env

docker compose --env-file deploy/.env --file deploy/compose.yml config --quiet
docker compose --env-file deploy/.env --file deploy/compose.yml up --build --detach
docker compose --env-file deploy/.env --file deploy/compose.yml ps

Invoke-RestMethod http://localhost:8080/health/ready
```

`deploy/.env.example` предназначен только для локального примера. Перед использованием вне изолированной машины замените все credentials и keys.

Остановка без удаления PostgreSQL volume:

```powershell
docker compose --env-file deploy/.env --file deploy/compose.yml down
```

## Сборка и тесты

```powershell
dotnet restore EcoBilling.slnx -warnaserror
dotnet build EcoBilling.slnx --configuration Release --no-restore
dotnet test EcoBilling.slnx --configuration Release --no-build
```

Без `ECOBILLING_TEST_POSTGRES_CONNECTION` PostgreSQL integration/E2E проверки явно пропускаются. Полный локальный прогон описан в [документации тестирования](docs/testing/README.md). GitHub Actions запускает тесты с настоящей временной PostgreSQL, проверяет Compose и собирает container images.

## Основная HTTP-поверхность

- `/api/v1/auth/*` — login, refresh, revoke, password setup;
- `/internal/v1/directors` — защищённый provisioning первого Director;
- `/api/v1/directors/me/profile` — Director self profile;
- `/api/v1/controllers/*` — directory, создание, assignments и worklist;
- `/api/v1/residents/*` — directory, создание и password reset;
- `/api/v1/me/*` — Resident self-service;
- `/api/v1/accounts/*`, `/addresses/*` — management lookup;
- `/api/v1/meters/*`, `/readings/*` — Meter и MeterReading;
- `/api/v1/tariffs/*` — Tariffs, versions и account assignments;
- `/api/v1/accounts/{accountId}/billing/{year}/{month}` — Billing v1;
- `/api/v1/payments/*` и account financial endpoints — Payments;
- `/api/v1/reports/*` — operational/financial summaries;
- `POST /api/v1/users/{userId}/sessions/revoke-all` — Director-only отзыв всех refresh sessions пользователя;
- `/health/live`, `/health/ready`, `/health` — health checks;
- `/swagger` — только в `Development`.

Точный контракт и ошибки описаны в [API документации](docs/api/README.md).

## Документация

Начальная точка — [docs/README.md](docs/README.md).

- [Статус проекта](docs/project-status.md)
- [Архитектура и ADR](docs/architecture/README.md)
- [Business rules v1](docs/business-rules/README.md)
- [API](docs/api/README.md)
- [Тестирование](docs/testing/README.md)
- [Конфигурация](docs/configuration/README.md)
- [Развёртывание](docs/deployment/README.md)
- [Операции](docs/operations/README.md)
- [Production readiness](docs/operations/production-readiness.md)
- [Security readiness](docs/security/README.md)
