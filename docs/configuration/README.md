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
| `UserAuthentication__Issuer` | Обязателен | Точный issuer пользовательских access JWT этого API. |
| `UserAuthentication__Audience` | Обязателен | Audience пользовательских клиентов округа. |
| `UserAuthentication__ActiveSigningKeyId` | Обязателен | `kid` активного ключа выпуска access JWT. |
| `UserAuthentication__SigningKeys__0__KeyId` | Обязателен | Уникальный `kid`; несколько элементов поддерживают безопасную ротацию. |
| `UserAuthentication__SigningKeys__0__SecretBase64` | Обязателен, секрет | Base64 минимум 32 случайных байт. Не хранится в Git. |
| `UserAuthentication__AccessTokenLifetime` | Необязателен | По умолчанию `00:15:00`. |
| `UserAuthentication__RefreshTokenLifetime` | Необязателен | По умолчанию `30.00:00:00`. |
| `UserAuthentication__MaximumFailedAttempts` | Необязателен | По умолчанию `5`. |
| `UserAuthentication__LockoutDuration` | Необязателен | По умолчанию `00:15:00`. |
| `UserAuthentication__MinimumPasswordLength` | Необязателен | По умолчанию `12`. |
| `UserAuthentication__MaximumPasswordLength` | Необязателен | По умолчанию `256`. |
| `Observability__OtlpEndpoint` | Необязателен | Абсолютный HTTP(S) URI OTLP collector. Пустое значение отключает экспорт. |
| `AllowedHosts` | Обязателен для окружения | Разделённый `;` allowlist host names, которые обслуживает API. Wildcard `*` запрещён для deployment. |
| `ReverseProxy__Enabled` | Необязателен | По умолчанию `false`. Включать только когда API реально стоит за trusted reverse proxy/ingress. |
| `ReverseProxy__ForwardLimit` | Обязателен при включённом proxy | Число доверенных proxy hops, минимум `1`; по умолчанию `1`. |
| `ReverseProxy__KnownProxies__0` | Обязателен при включённом proxy | Точный IP доверенного proxy hop. Можно добавить следующие индексы для нескольких адресов. |
| `ASPNETCORE_ENVIRONMENT` | Необязателен | В production не должен иметь значение `Development`; OpenAPI включён только в `Development`. |
| `ASPNETCORE_HTTP_PORTS` | Необязателен | В Compose API слушает внутренний порт `8080`. |
| `ReverseProxy__Enabled` | Необязателен | По умолчанию `false`. Включать только за реальным ingress/reverse proxy. |
| `ReverseProxy__ForwardLimit` | Необязателен | Число доверенных proxy hops, по умолчанию `1`. |
| `ReverseProxy__KnownProxies__0` | Обязателен при включённом reverse proxy | Точный IP доверенного proxy. Непарсируемые/пустые значения блокируют запуск. |

API завершает запуск с ошибкой, если обязательная строка подключения, fingerprint key, service JWT issuer/audience/public key или user JWT issuer/audience/signing key отсутствуют.

## EcoBilling.Worker

| Ключ конфигурации | Обязательность | Назначение |
|---|---|---|
| `ConnectionStrings__EcoBilling` | Обязателен, секрет | Строка подключения runtime к PostgreSQL. |
| `Worker__Execution__MaxAttempts` | Необязателен | Общее число попыток задания, минимум `1`; по умолчанию `3`. |
| `Worker__Execution__RetryDelay` | Необязателен | Неотрицательная задержка `TimeSpan`; по умолчанию `00:00:05`. |
| `Worker__Schedules__MonthlyBillingEnabled` | Необязателен | По умолчанию `false`; включает idempotent monthly billing batch. |
| `Worker__Schedules__MonthlyBillingInterval` | Необязателен | Интервал запуска проверки billing batch; по умолчанию `1.00:00:00`. |
| `Worker__Schedules__OutboxEnabled` | Необязателен | По умолчанию `false`; включать только после регистрации реального publisher. |
| `Worker__Schedules__OutboxInterval` | Необязателен | Интервал Outbox batch; по умолчанию `00:01:00`. |
| `Worker__Schedules__OutboxBatchSize` | Необязателен | Размер batch `1..1000`, по умолчанию `100`. |
| `Observability__OtlpEndpoint` | Необязателен | Тот же OTLP collector, что и для API. |
| `DOTNET_ENVIRONMENT` | Необязателен | В production не должен иметь значение `Development`. |

Worker не применяет миграции. В нём зарегистрированы Monthly Billing и Outbox jobs; оба schedule по умолчанию выключены. Monthly Billing можно включать после проверки billing policy на staging, Outbox — только после подключения реального `IOutboxMessagePublisher`.

## Design-time и тесты

| Переменная | Назначение |
|---|---|
| `ECOBILLING_DESIGN_TIME_CONNECTION` | Строка подключения для `dotnet ef`. Пользователь должен иметь права на изменение схемы целевой базы. |

Для локального запуска API можно скопировать `src/EcoBilling.Api/appsettings.Local.example.json` в `src/EcoBilling.Api/appsettings.Local.json` и заменить заглушки. `appsettings.Local.json` игнорируется Git и загружается только при `LocalConfiguration__Enabled=true` или при подключённом debugger. Штатные профили `http` и `https` включают этот локальный режим и открывают Swagger. Переменные окружения и secret manager остаются обязательным способом конфигурации production.
| `ECOBILLING_TEST_POSTGRES_CONNECTION` | Административная строка тестовой PostgreSQL. Тесты создают и удаляют отдельные базы, поэтому нужны права `CREATE DATABASE`. |

Production credentials нельзя использовать для локальных или CI-тестов.

## Переменные Docker Compose

`deploy/compose.yml` читает:

- обязательные для локального Compose `POSTGRES_PASSWORD`, `DIRECTOR_PROVISIONING_FINGERPRINT_KEY`, `INTERNAL_SERVICE_PUBLIC_KEY_PEM`, `USER_AUTH_SIGNING_KEY` и `ALLOWED_HOSTS`;
- локальные идентификаторы `INTERNAL_SERVICE_ISSUER`, `INTERNAL_SERVICE_AUDIENCE`, `INTERNAL_SERVICE_KEY_ID`, `USER_AUTH_ISSUER`, `USER_AUTH_AUDIENCE` и `USER_AUTH_KEY_ID`;
- `ECOBILLING_API_PORT` для host-порта API, по умолчанию `8080`;
- `ASPNETCORE_ENVIRONMENT` и `DOTNET_ENVIRONMENT`, по умолчанию `Production`;
- необязательный `OTLP_ENDPOINT`;
- `REVERSE_PROXY_ENABLED`, `REVERSE_PROXY_FORWARD_LIMIT`, `TRUSTED_PROXY_IP` для trusted forwarded headers;
- `WORKER_MONTHLY_BILLING_ENABLED`, `WORKER_MONTHLY_BILLING_INTERVAL`, `WORKER_OUTBOX_ENABLED`, `WORKER_OUTBOX_INTERVAL`, `WORKER_OUTBOX_BATCH_SIZE`.

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

При ротации пользовательского JWT-ключа сначала добавьте новый элемент `SigningKeys`, затем переключите `ActiveSigningKeyId`. Старый ключ удаляется только после максимального срока ранее выпущенного access token с учётом clock skew. Ротация signing key не заменяет отзыв refresh token family.


## Production PostgreSQL credentials

Локальный Compose использует один credential для простоты. Production не должен повторять эту модель.

В `deploy/postgres` находятся:

- `bootstrap-production-roles.sql` — создаёт/обновляет `ecobilling_migrator` и `ecobilling_runtime`;
- `grant-runtime.sql` — выдаёт runtime data grants после миграций и запрещает UPDATE/DELETE для AuditLog.

Production deployment передаёт:

- migration connection только migration job;
- runtime connection только API/Worker.

См. [production readiness checklist](../operations/production-readiness.md).
