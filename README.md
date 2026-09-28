# EcoBilling

EcoBilling — backend системы учёта и оплаты воды для одного округа. Каждый округ разворачивает отдельные экземпляры API, Worker и PostgreSQL.

Центральная маршрутизация по коду округа, системные администраторы и реестр округов относятся к отдельному проекту `ecobilling-control` и не входят в этот репозиторий.

## Текущий статус

Репозиторий содержит проверенную основу модульного монолита, PostgreSQL persistence, защищённый внутренний provisioning первого директора, пользовательскую аутентификацию, аудит, observability, контейнерный запуск, CI и E2E-тесты.

Создания жителей и контроллеров, назначений контроллеров, команд работы со счётчиками и показаниями, расчёта начислений, интеграции с платёжным провайдером и реальных фоновых заданий пока нет. Эти функции нельзя считать готовыми только по наличию доменных и persistence-моделей.

Полная матрица реализованного и отложенного объёма находится в [статусе проекта](docs/project-status.md). Открытые бизнес-решения перечислены в [реестре решений](docs/architecture/open-decisions.md). [Финальная проверка безопасности](docs/security/README.md) завершена с решением **NO-GO** для production до закрытия перечисленных эксплуатационных и supply-chain блокеров.

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
- Docker Engine или Docker Desktop с Docker Compose v2 для полного локального стека;
- PowerShell для приведённых ниже примеров команд.

## Быстрый запуск через Docker Compose

Создайте локальную конфигурацию и замените все значения-заглушки:

```powershell
Copy-Item deploy/.env.example deploy/.env
notepad deploy/.env
docker compose --env-file deploy/.env --file deploy/compose.yml config --quiet
docker compose --env-file deploy/.env --file deploy/compose.yml up --build --detach
docker compose --env-file deploy/.env --file deploy/compose.yml ps
Invoke-RestMethod http://localhost:8080/health/ready
```

Пример публичного RSA-ключа позволяет запустить API, но соответствующий private key не хранится в репозитории. Для вызова внутреннего provisioning endpoint нужен EcoBilling.Control с согласованной парой ключей.

Остановка сохраняет named volume PostgreSQL:

```powershell
docker compose --env-file deploy/.env --file deploy/compose.yml down
```

Подробности: [развёртывание](docs/deployment/README.md), [конфигурация](docs/configuration/README.md), [эксплуатационный runbook](docs/operations/README.md) и [security readiness](docs/security/README.md).

## Сборка и тесты

```powershell
dotnet restore EcoBilling.slnx -warnaserror
dotnet build EcoBilling.slnx --configuration Release --no-restore
dotnet test EcoBilling.slnx --configuration Release --no-build
```

Без `ECOBILLING_TEST_POSTGRES_CONNECTION` тесты, требующие PostgreSQL, явно пропускаются. Для полного прогона с отдельными временными базами используйте инструкцию [тестирования](docs/testing/README.md). CI выполняет полный набор с PostgreSQL и проверяет Docker Compose.

## HTTP-поверхность

- `GET /health/live` — liveness процесса;
- `GET /health/ready` и `GET /health` — readiness с PostgreSQL;
- `POST /internal/v1/directors` — защищённое идемпотентное создание первого директора.
- `POST /api/v1/auth/login`, `/refresh`, `/revoke` и `/setup-password` — пользовательская аутентификация и жизненный цикл токенов.
- `GET /swagger` — интерактивное тестирование API в окружении `Development` с поддержкой JWT Bearer через `Authorize`.

Точный контракт, требования JWT, ответы и ошибки описаны в [документации API](docs/api/README.md). OpenAPI доступен только в окружении `Development`; публичная регистрация отсутствует.

## Документация

Начальная точка — [docs/README.md](docs/README.md). Там собраны ссылки на архитектуру и ADR, бизнес-правила, схему PostgreSQL, конфигурацию, API, тестирование, развёртывание и эксплуатацию.
