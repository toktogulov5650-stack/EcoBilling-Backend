# Безопасность и готовность к выпуску

Проверка выполнена 28 сентября 2026 года для состояния репозитория после этапа 21. Она охватывает исходный код, конфигурацию, зависимости NuGet, CI, контейнерные манифесты, документированные секреты и существующие автоматические тесты. Это проверка текущей технической основы, а не аудит ещё не реализованных пользовательских сценариев.

## Решение о выпуске

Текущее решение для production: **NO-GO**.

Состояние подходит для разработки, integration и изолированного staging. Production-выпуск блокируют не ошибки сборки, а незакрытые эксплуатационные решения и уязвимости используемого контейнерного образа PostgreSQL. Дополнительно продукт пока не содержит пользовательскую аутентификацию и основные бизнес-сценарии, перечисленные в [статусе проекта](../project-status.md).

## Подтверждённые меры

- NuGet audit включён централизованно для прямых и транзитивных зависимостей с уровнем `low`; текущая проверка advisories не нашла уязвимых пакетов.
- GitHub Actions закреплены полными commit SHA, workflow имеют минимальные read-only permissions и выполняют Release build/test, PostgreSQL integration и проверку Compose.
- В отслеживаемых файлах не обнаружены private keys, production connection strings или реальные production-секреты. `deploy/.env`, локальные appsettings и user secrets игнорируются.
- API не принимает произвольный `Host`: `AllowedHosts` не может быть wildcard, а Compose требует явный `ALLOWED_HOSTS`.
- Внутренний endpoint защищён RS256 JWT с точными issuer/audience, обязательными `kid`, scope и `jti`; replay и идемпотентность защищены в PostgreSQL.
- Production-код не включает sensitive-data logging. Problem Details не раскрывает исключения или строки подключения.
- Worker не передаёт объект исключения и его сообщение в лог; остаётся только технический тип ошибки.
- Runtime-контейнеры non-root, read-only, без Linux capabilities и с `no-new-privileges`; миграции выполняет отдельный service.

## Блокеры production

| Блокер | Требуемое закрытие |
|---|---|
| Образ `postgres:17-alpine` | На момент проверки Docker Scout обнаруживает 2 Critical и 21 High fixable CVE в Go standard library внутри актуального локального образа. Нужен обновлённый официальный образ либо документированное VEX/risk acceptance после проверки достижимости. |
| Секреты | Выбрать production secret store, разграничить доступ и утвердить процедуры выдачи, ротации и отзыва. |
| PostgreSQL-роли | Разделить migration и runtime credentials, определить минимальные grants и проверить их тестовым развёртыванием. |
| Сетевая граница | Настроить TLS termination, доверенные proxy/ingress, forwarded headers, allowlist hosts и сетевые политики для конкретного окружения. |
| Данные и восстановление | Утвердить backup/restore, выполнить пробное восстановление и назначить RPO, RTO, SLA и сроки хранения. |
| Наблюдаемость | Выбрать backend/collector, sampling, dashboards, alerts, получателей и сроки хранения telemetry. |
| Выпуск контейнеров | Просканировать окончательные immutable images после сборки, установить CVE threshold, SBOM/VEX и срок устранения уязвимостей. |

Локальные `ecobilling-api`, `ecobilling-worker` и `ecobilling-migrations` успешно собраны. Их отправка или передача SBOM/метаданных внешнему scanner не выполнялась без отдельного разрешения владельца кода; поэтому отсутствие CVE в этих трёх images не подтверждено.

## Остаточные риски

- Текущая HTTP-поверхность не включает пользовательский login, выпуск access/refresh token, блокировку аккаунта и rate limiting. Эти меры должны проектироваться вместе с будущими endpoints, а не считаться реализованными заранее.
- Обновления test tooling до следующих major-версий доступны, но не содержат известного security advisory в текущем audit. Они отложены до отдельного совместимого изменения.
- Статический поиск секретов и dependency audit не заменяют централизованный secret scanning, SAST/DAST и периодический пересмотр threat model.

## Воспроизводимая проверка

```powershell
dotnet restore EcoBilling.slnx -warnaserror -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet package list --project EcoBilling.slnx --include-transitive --vulnerable --no-restore
dotnet build EcoBilling.slnx --configuration Release --no-restore
dotnet test EcoBilling.slnx --configuration Release --no-build
docker compose --env-file deploy/.env.example --file deploy/compose.yml config --quiet
```

Решение может быть изменено на GO только после закрытия применимых блокеров с проверяемыми доказательствами и повторного полного прогона.
