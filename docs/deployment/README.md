# Развёртывание

Каждый округ получает отдельные экземпляры API, Worker и PostgreSQL. Файлы локального контейнерного запуска находятся в `deploy`.

## Docker Compose

Для локального запуска создайте игнорируемый Git файл окружения и замените значения-заглушки:

```powershell
Copy-Item deploy/.env.example deploy/.env
notepad deploy/.env
```

Публичный RSA-ключ в примере не является секретом и не имеет сохранённого private key. Для рабочего контракта укажите public key соответствующего EcoBilling.Control. Пароль PostgreSQL и `DIRECTOR_PROVISIONING_FINGERPRINT_KEY` всегда заменяйте; production-значения должны поступать из внешнего secret store или отдельного Compose override.

Проверка и запуск:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yml config --quiet
docker compose --env-file deploy/.env -f deploy/compose.yml up --build --detach
docker compose --env-file deploy/.env -f deploy/compose.yml ps
Invoke-WebRequest http://localhost:8080/health/ready
```

Логи и остановка:

```powershell
docker compose --env-file deploy/.env -f deploy/compose.yml logs --follow api worker
docker compose --env-file deploy/.env -f deploy/compose.yml down
```

`down` сохраняет named volume PostgreSQL. Команда `down --volumes` удаляет локальные данные и должна использоваться только осознанно.

Порядок старта:

1. PostgreSQL проходит `pg_isready`.
2. Одноразовый service `migrations` применяет EF Core migrations.
3. API и Worker запускаются только после успешных миграций.
4. API считается готовым после `/health/ready`.

API публикуется на `ECOBILLING_API_PORT` (`8080` по умолчанию). PostgreSQL наружу не публикуется. Runtime-контейнеры работают non-root, с read-only root filesystem, writable `/tmp`, удалёнными capabilities, `no-new-privileges` и 30-секундным graceful shutdown. TLS должен завершаться reverse proxy или ingress перед API.

В production не задавайте `ASPNETCORE_ENVIRONMENT` и `DOTNET_ENVIRONMENT` как `Development`; значения Compose по умолчанию — `Production`. Dockerfile фиксируют patch-версии .NET, поэтому их следует обновлять вместе с плановым обновлением SDK/runtime и полной проверкой образов.

## Worker

Worker требует строку подключения `ConnectionStrings:EcoBilling`. В окружении её следует передавать как секрет, например через `ConnectionStrings__EcoBilling`; строка подключения не хранится в `appsettings.json`.

Общие параметры выполнения находятся в секции `Worker:Execution`:

- `MaxAttempts` — общее максимальное число попыток, не меньше `1`;
- `RetryDelay` — неотрицательная задержка между попытками в формате `TimeSpan`.

Значения по умолчанию проекта — три попытки и пять секунд. Worker корректно передаёт сигнал остановки выполняемому заданию. Worker самостоятельно миграции не применяет; в Compose это делает отдельный одноразовый service. Реальные фоновые задания на этапе ADR-0016 не включены.

## Внутренняя аутентификация API

Для запуска API обязательны следующие параметры окружения:

- `ConnectionStrings__EcoBilling` — строка подключения PostgreSQL;
- `InternalServiceAuthentication__Issuer` — точный issuer EcoBilling.Control;
- `InternalServiceAuthentication__Audience` — уникальная audience этого экземпляра EcoBilling;
- `InternalServiceAuthentication__SigningKeys__0__KeyId` — идентификатор публичного ключа;
- `InternalServiceAuthentication__SigningKeys__0__PublicKeyPem` — RSA public key в PEM;
- `DirectorProvisioning__RequestFingerprintKey` — секрет из минимум 32 случайных байт в Base64.

Для ротации добавьте следующий публичный ключ новым индексом `SigningKeys`, разверните конфигурацию EcoBilling, переключите Control на новый `kid`, дождитесь окончания максимального срока JWT с учётом clock skew и только затем удалите старый ключ.

Закрытый RSA-ключ существует только в EcoBilling.Control. HMAC-ключ fingerprint и строка подключения хранятся в secret store и не коммитятся. Миграции автоматически при запуске API не применяются.

## Наблюдаемость

API и Worker всегда пишут структурированные JSON-логи в стандартный вывод. Они не журналируют тела HTTP-запросов, токены, пароли и строки подключения.

API предоставляет:

- `/health/live` — процесс запущен; PostgreSQL не проверяется;
- `/health/ready` — экземпляр готов принимать трафик и PostgreSQL доступна;
- `/health` — совместимый alias readiness.

Для отправки логов, traces и metrics в совместимый OpenTelemetry collector задайте абсолютный HTTP(S) адрес:

```text
Observability__OtlpEndpoint=http://otel-collector:4317
```

Если значение не задано, OTLP-экспорт не запускается. Выбор collector/backend, sampling, dashboards, alerts и сроки хранения остаются частью production deployment. Worker публикует ошибки и длительность заданий в logs/metrics/traces, но отдельный HTTP health server для него будет определён вместе с Docker-моделью.
