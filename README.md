# EcoBilling

EcoBilling — основной backend системы учёта и оплаты воды. Каждый округ разворачивает отдельный экземпляр EcoBilling и отдельную PostgreSQL.

Центральная маршрутизация по коду округа относится к отдельному проекту `ecobilling-control` и не входит в этот репозиторий.

## Проекты

- `EcoBilling.Api` — публичные и внутренние HTTP endpoints.
- `EcoBilling.Modules` — бизнес-модули и правила предметной области.
- `EcoBilling.Infrastructure` — база данных, безопасность и внешние интеграции.
- `EcoBilling.Worker` — фоновые задания, запускающие сценарии модулей.
- `EcoBilling.SharedKernel` — небольшие общие технические типы.

## Зависимости

```text
Api ─────┬─→ Infrastructure ─→ Modules ─→ SharedKernel
         └─→ Modules

Worker ──┬─→ Infrastructure
         └─→ Modules
```

## Команды

```powershell
dotnet restore EcoBilling.slnx
dotnet build EcoBilling.slnx
dotnet test EcoBilling.slnx
dotnet run --project src/EcoBilling.Api/EcoBilling.Api.csproj
docker compose --env-file deploy/.env -f deploy/compose.yml up --build --detach
```

После запуска API доступны `/health/live` для liveness и `/health/ready` для readiness PostgreSQL. `/health` сохраняется как alias readiness.

## Статус

Этап 17 добавил production-oriented Dockerfile для API и Worker, non-root/read-only runtime, обязательную внешнюю конфигурацию секретов, health checks и Compose-стек PostgreSQL → migrations → API/Worker. PostgreSQL не публикуется наружу, а production TLS остаётся обязанностью reverse proxy или ingress.

Этап 16 добавил структурированные JSON-логи, correlation/trace context, OpenTelemetry traces и metrics для API, PostgreSQL и Worker, а также раздельные liveness/readiness checks. OTLP-экспорт включается только конфигурацией; collector, dashboards и alerts будут определены при развёртывании.

Этап 15 добавил атомарный append-only аудит provisioning директора и persistence-основу Outbox. Реальные Outbox-события и фоновый dispatcher не создаются до утверждения внешнего контракта и политики обработки.

Созданы архитектурный каркас, примитивы `Error`/`Result`, исполняемые проверки графа зависимостей, Identity с PostgreSQL persistence, профили Resident и Controller, Account с обязательными связями с Resident и Address, базовые модели Meter и MeterReading, Tariff с неизменяемыми версиями ставок, минимальная запись Charge без формулы расчёта, подтверждённый Payment с idempotency key, read-only операционная сводка Reports, общий механизм выполнения Worker и защищённый идемпотентный контракт EcoBilling.Control → EcoBilling для первоначального директора. Пользовательская HTTP-аутентификация, завершение первичной установки пароля, создание жителей и контроллеров, назначения контроллеров, жизненный цикл счётчиков, сценарии внесения и исправления показаний, управление тарифами, финансовое состояние счёта, расчёт начислений, провайдерский платёжный workflow, финансовые отчёты и реальные фоновые задания будут проектироваться отдельными этапами после утверждения правил.
