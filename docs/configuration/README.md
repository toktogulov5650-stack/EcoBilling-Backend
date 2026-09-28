# Конфигурация

EcoBilling использует стандартную конфигурацию .NET. В переменных окружения вложенные ключи разделяются двойным подчёркиванием: `ConnectionStrings:EcoBilling` передаётся как `ConnectionStrings__EcoBilling`.

Секреты не хранятся в `appsettings.json`, Git, логах или образах. Файл `deploy/.env` предназначен только для локальной машины и игнорируется Git; `deploy/.env.example` содержит исключительно пример запуска.

## EcoBilling.Api

| Ключ конфигурации | Обязательность | Назначение |
|---|---|---|
| `ConnectionStrings__EcoBilling` | Обязателен, секрет | Строка подключения runtime к PostgreSQL. |
| `DirectorProvisioning__RequestFingerprintKey` | Обязателен, секрет | Base64 от минимум 32 случайных байт для HMAC fingerprint идемпотентного запроса. |
| `InternalServiceAuthentication__Issuer` | Обязателен | Точный issuer EcoBilling.Control. |
| `InternalServiceAuthentication__Audience` | Обязателен | Audience конкретного экземпляра EcoBilling. |
| `InternalServiceAuthentication__SigningKeys__0__KeyId` | Обязателен | `kid` доверенного публичного RSA-ключа. Индекс можно повторять для ротации ключей. |
| `InternalServiceAuthentication__SigningKeys__0__PublicKeyPem` | Обязателен, доверенная конфигурация | Только публичный RSA-ключ. Private key остаётся в EcoBilling.Control. |
| `InternalServiceAuthentication__MaximumTokenLifetime` | Необязателен | Максимальная жизнь service JWT; по умолчанию `00:02:00`. |
| `InternalServiceAuthentication__ClockSkew` | Необязателен | Допустимое расхождение часов; по умолчанию `00:00:30`. |
| `Observability__OtlpEndpoint` | Необязателен | Абсолютный HTTP(S) URI OTLP collector. Пустое значение отключает экспорт. |
| `AllowedHosts` | Обязателен для окружения | Разделённый `;` allowlist host names, которые обслуживает API. Wildcard `*` запрещён для deployment. |
| `ASPNETCORE_ENVIRONMENT` | Необязателен | В production не должен иметь значение `Development`; OpenAPI включён только в `Development`. |
| `ASPNETCORE_HTTP_PORTS` | Необязателен | В Compose API слушает внутренний порт `8080`. |

API завершает запуск с ошибкой, если обязательная строка подключения, fingerprint key, issuer, audience или хотя бы один корректный публичный ключ отсутствуют.

## EcoBilling.Worker

| Ключ конфигурации | Обязательность | Назначение |
|---|---|---|
| `ConnectionStrings__EcoBilling` | Обязателен, секрет | Строка подключения runtime к PostgreSQL. |
| `Worker__Execution__MaxAttempts` | Необязателен | Общее число попыток задания, минимум `1`; по умолчанию `3`. |
| `Worker__Execution__RetryDelay` | Необязателен | Неотрицательная задержка `TimeSpan`; по умолчанию `00:00:05`. |
| `Observability__OtlpEndpoint` | Необязателен | Тот же OTLP collector, что и для API. |
| `DOTNET_ENVIRONMENT` | Необязателен | В production не должен иметь значение `Development`. |

Worker не применяет миграции и пока не регистрирует реальные задания.

## Design-time и тесты

| Переменная | Назначение |
|---|---|
| `ECOBILLING_DESIGN_TIME_CONNECTION` | Строка подключения для `dotnet ef`. Пользователь должен иметь права на изменение схемы целевой базы. |
| `ECOBILLING_TEST_POSTGRES_CONNECTION` | Административная строка тестовой PostgreSQL. Тесты создают и удаляют отдельные базы, поэтому нужны права `CREATE DATABASE`. |

Production credentials нельзя использовать для локальных или CI-тестов.

## Переменные Docker Compose

`deploy/compose.yml` читает:

- обязательные `POSTGRES_PASSWORD`, `DIRECTOR_PROVISIONING_FINGERPRINT_KEY`, `INTERNAL_SERVICE_PUBLIC_KEY_PEM` и `ALLOWED_HOSTS`;
- локальные идентификаторы `INTERNAL_SERVICE_ISSUER`, `INTERNAL_SERVICE_AUDIENCE` и `INTERNAL_SERVICE_KEY_ID`;
- `ECOBILLING_API_PORT` для host-порта API, по умолчанию `8080`;
- `ASPNETCORE_ENVIRONMENT` и `DOTNET_ENVIRONMENT`, по умолчанию `Production`;
- необязательный `OTLP_ENDPOINT`.

`ALLOWED_HOSTS` должен содержать внешние host names конкретного окружения и внутреннее имя `api`, если health check вызывается внутри Compose. Не используйте `*`; TLS proxy также должен проверять и передавать ожидаемый Host.

Локальный 32-байтовый fingerprint key можно сгенерировать PowerShell-командой и сразу перенести в защищённое хранилище:

```powershell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

Не публикуйте результат в issue, CI output или истории shell. Пример ключа из `.env.example` необходимо заменить перед любым использованием вне изолированной локальной среды.

## Ротация service-ключа

1. Добавьте новый публичный ключ с новым индексом `SigningKeys` и уникальным `KeyId`.
2. Разверните конфигурацию EcoBilling.
3. Переключите EcoBilling.Control на новый private key и `kid`.
4. Дождитесь максимального срока ранее выпущенного JWT с учётом clock skew.
5. Удалите старый публичный ключ.

Механизм production secret store выбирается при развёртывании и в репозитории не фиксируется случайным значением.
