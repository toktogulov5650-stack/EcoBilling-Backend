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
```

После запуска API техническая проверка доступна по адресу `/health`.

## Статус

Созданы архитектурный каркас, примитивы `Error`/`Result`, исполняемые проверки графа зависимостей, Identity с PostgreSQL persistence, профили Resident и Controller, Account с обязательными связями с Resident и Address, базовые модели Meter и MeterReading, Tariff с неизменяемыми версиями ставок, минимальная запись Charge без формулы расчёта, подтверждённый Payment с idempotency key, read-only операционная сводка Reports и общий механизм выполнения Worker с отменой, ограниченными повторами, логами и метриками. HTTP-аутентификация, полное создание пользователей, назначения контроллеров, жизненный цикл счётчиков, сценарии внесения и исправления показаний, управление тарифами, финансовое состояние счёта, токены, расчёт начислений, провайдерский платёжный workflow, финансовые отчёты и реальные фоновые задания будут проектироваться отдельными этапами после утверждения правил.
