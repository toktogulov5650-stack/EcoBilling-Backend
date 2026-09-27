# ADR-0020: контейнерное развёртывание одного округа

- Статус: принято
- Дата: 2026-09-28

## Контекст

Один округ должен запускать отдельные API, Worker и PostgreSQL. Исходные Docker-файлы использовали неверный ключ строки подключения, известный пароль по умолчанию, не передавали обязательную внутреннюю аутентификацию, не различали готовность сервисов и не фиксировали non-root runtime. Чистая PostgreSQL также оставалась без схемы, потому что API и Worker намеренно не применяют миграции при старте.

## Решение

- API и Worker собираются отдельными multi-stage Dockerfile на закреплённых patch-версиях .NET 10.
- Restore использует только project files и централизованные package props, после чего исходники копируются отдельным слоем. Тесты, документация, Git и локальные секреты исключены через `.dockerignore`.
- Runtime API и Worker запускается встроенным непривилегированным пользователем `$APP_UID`, с `SIGTERM`, read-only root filesystem, writable `/tmp`, удалёнными Linux capabilities и `no-new-privileges` в Compose.
- API image имеет liveness health check `/health/live`; Compose проверяет readiness `/health/ready`.
- Worker health check подтверждает жизнь PID 1. Это достаточно, пока в Worker нет реального hosted task; readiness конкретных заданий появится вместе с их контрактами.
- PostgreSQL не публикуется на host и доступна только во внутренней сети Compose. Данные сохраняются в named volume.
- Пароль PostgreSQL, fingerprint key и публичный ключ Control обязательны: Compose прекращает разбор, если они отсутствуют. Известного пароля по умолчанию нет.
- `deploy/.env.example` предназначен только для локальной разработки. Реальный `deploy/.env` игнорируется Git, а production использует отдельный secret store или Compose override.
- Отдельный одноразовый service `migrations` применяет EF Core migrations после готовности PostgreSQL. API и Worker запускаются только после его успешного завершения и сами схему не меняют.
- API и Worker получают только `ConnectionStrings__EcoBilling`; прежний неверный `ConnectionStrings__Database` удалён.
- OTLP endpoint остаётся необязательным и передаётся через environment configuration.

## Последствия

- `docker compose up --build` создаёт воспроизводимый локальный стек с подготовленной схемой и контролируемым порядком запуска.
- Ошибка миграции блокирует запуск API и Worker вместо работы с неполной схемой.
- Для production должны быть заменены все значения из `.env.example`; пример публичного ключа не имеет соответствующего сохранённого private key.
- TLS завершается внешним reverse proxy или ingress; контейнер API слушает внутренний HTTP-порт `8080`.
- Docker image build и полный smoke test требуют запущенного Docker daemon.
