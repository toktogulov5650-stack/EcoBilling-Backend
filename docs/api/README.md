# API

## Текущая поверхность

| Метод и путь | Доступ | Назначение |
|---|---|---|
| `POST /internal/v1/directors` | Отдельный service JWT | Первоначальное создание единственного директора округа. |
| `POST /api/v1/auth/login` | Anonymous | Вход по email или номеру лицевого счёта и выпуск пары токенов. |
| `POST /api/v1/auth/refresh` | Anonymous, refresh token в body | Одноразовая ротация refresh token и выпуск нового access token. |
| `POST /api/v1/auth/revoke` | Anonymous, refresh token в body | Идемпотентный отзыв всей token family. |
| `POST /api/v1/auth/setup-password` | Anonymous, начальная тайна в body | Однократная замена начальной тайны Director/Controller. |
| `GET /health/live` | Anonymous | Liveness процесса без зависимости от PostgreSQL. |
| `GET /health/ready` | Anonymous | Readiness процесса и PostgreSQL. |
| `GET /health` | Anonymous | Совместимый alias readiness. |

Публичной регистрации нет. Неизвестный маршрут получает безопасный RFC 7807 `404` с кодом `request.not_found`.

## Пользовательская аутентификация

`POST /api/v1/auth/login` принимает `loginType` со значением `email` или `accountNumber`, `login` и `password`. Успешный ответ содержит `Bearer` access token, его UTC-срок, одноразовый refresh token, срок refresh token и роль. Access token по умолчанию действует 15 минут, refresh token — 30 дней.

Refresh token хранится только как SHA-256 hash. `POST /api/v1/auth/refresh` атомарно помечает предъявленную сессию использованной и создаёт замену в той же семье. Повтор уже использованного token отзывает всю семью, включая выданную замену. `POST /api/v1/auth/revoke` также отзывает семью и всегда идемпотентно возвращает `204`.

Пять подряд неверных паролей блокируют account на 15 минут. Ответ остаётся одинаковым для неизвестного login, неправильного password и заблокированного account: `401 auth.invalid_credentials`.

Director и Controller, созданные с начальной тайной, сначала вызывают `POST /api/v1/auth/setup-password`. Новый пароль содержит 12–256 символов и не может совпадать с начальной тайной. До успешной замены login возвращает `403 auth.password_setup_required`. Resident не использует self-service setup: его пароль устанавливает или сбрасывает Director.

Access JWT подписывается HS256, содержит `kid`, `sub`, `jti`, `iat`, `exp` и `role`, проверяется по точным issuer/audience и не содержит refresh token или пароль. Raw access/refresh tokens и credentials не журналируются.

## Внутреннее создание директора

```http
POST /internal/v1/directors
Authorization: Bearer <service-jwt>
Idempotency-Key: <opaque-operation-key>
X-Correlation-Id: <optional-trace-id>
Content-Type: application/json
```

```json
{
  "fullName": "Ada Lovelace",
  "email": "director@example.com",
  "initialCredential": "<high-entropy-one-time-credential>"
}
```

Успешный ответ имеет статус `201 Created`:

```json
{
  "directorId": "00000000-0000-0000-0000-000000000000",
  "operationId": "00000000-0000-0000-0000-000000000000",
  "status": "created"
}
```

Заголовок `Idempotency-Replayed` равен `false` для создания и `true` для успешного повтора. `Idempotency-Key` обязателен, регистрозависим и имеет длину не более 200 символов. Повтор должен использовать новый JWT и прежний `Idempotency-Key`.

JWT принимается только с RS256 и обязан содержать:

- известный `kid`, соответствующий настроенному публичному RSA-ключу;
- точные `iss` и `aud`;
- `iat` и `exp`, причём `exp` позже `iat`, а срок не превышает настроенный максимум;
- уникальный `jti`;
- scope `ecobilling.directors.provision`.

Максимальная жизнь токена по умолчанию — две минуты, clock skew — 30 секунд. Каждый `jti` атомарно принимается только один раз и сохраняется до `exp + clock skew`.

Начальная учётная тайна должна содержать 32–256 символов без пробельных символов. Она хешируется и не возвращается. Директор остаётся в состоянии обязательной первичной установки пароля; обычный вход до её завершения запрещён.

Ошибки используют `application/problem+json` с расширениями `code`, `traceId` и, для ошибок проверки, `validationErrors`. Основные коды:

- `service.unauthorized` — JWT отсутствует, неверен или уже использован;
- `operation.invalid_idempotency_key` — заголовок отсутствует или некорректен;
- `operation.idempotency_conflict` — ключ уже связан с другим запросом;
- `director.already_exists` — директор или email уже существует;
- `director.invalid_full_name`, `auth.invalid_login`, `director.invalid_initial_credential` — неверные поля запроса.

Открытая начальная тайна, JWT, закрытый ключ Control и HMAC-ключ fingerprint не должны попадать в логи, telemetry или committed configuration.

## Ошибки и корреляция

Ошибки имеют content type `application/problem+json` и стандартные поля RFC 7807. Расширение `code` предназначено для машинной обработки, `traceId` совпадает с принятым correlation ID, а `validationErrors` присутствует для ошибок проверки.

```json
{
  "title": "Validation failed",
  "status": 400,
  "detail": "The request is invalid.",
  "instance": "/internal/v1/directors",
  "code": "validation.failed",
  "traceId": "control-operation-42",
  "validationErrors": {
    "request": ["The request body is invalid."]
  }
}
```

Клиент может передать `X-Correlation-Id` длиной до 128 символов без управляющих символов. Некорректное значение не отражается: сервер использует безопасный собственный идентификатор. Итоговый идентификатор возвращается в заголовке `X-Correlation-Id`, ProblemDetails и audit записи provisioning.

## Техническое состояние

```http
GET /health/live
GET /health/ready
GET /health
```

`/health/live` подтверждает работу процесса и не зависит от PostgreSQL. `/health/ready` и совместимый `/health` возвращают `200 OK`, только когда PostgreSQL доступна; иначе возвращается `503 Service Unavailable`.

Ответ содержит `status`, общую длительность и массив `checks` с именем, статусом и длительностью каждой проверки. Исключения, строка подключения и иные внутренние детали не возвращаются. Эти endpoints не требуют аутентификации и исключены из OpenAPI.

## OpenAPI

В окружении `Development` интерактивный Swagger UI доступен по адресу `GET /swagger`, а JSON-контракт — как `GET /swagger/v1/swagger.json`. Сохранён и нативный документ `GET /openapi/v1.json`. Swagger поддерживает JWT Bearer через кнопку `Authorize`: используйте access token, возвращённый `POST /api/v1/auth/login`.

В `Testing` и `Production` Swagger UI и OpenAPI endpoints не публикуются. Health endpoints исключены из документа; внутренний provisioning остаётся помеченным тегом `Internal` и не становится публичным контрактом из-за наличия в OpenAPI.
