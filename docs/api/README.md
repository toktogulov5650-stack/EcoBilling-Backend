# API

## Текущая поверхность

| Метод и путь | Доступ | Назначение |
|---|---|---|
| `POST /internal/v1/directors` | Отдельный service JWT | Первоначальное создание единственного директора округа. |
| `GET /health/live` | Anonymous | Liveness процесса без зависимости от PostgreSQL. |
| `GET /health/ready` | Anonymous | Readiness процесса и PostgreSQL. |
| `GET /health` | Anonymous | Совместимый alias readiness. |

Публичных `/api/v1` endpoints, регистрации, пользовательского входа и выпуска токенов пока нет. Неизвестный маршрут получает безопасный RFC 7807 `404` с кодом `request.not_found`.

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

В окружении `Development` документ доступен как `GET /openapi/v1.json`. В `Testing` и `Production` OpenAPI endpoint не публикуется. Health endpoints исключены из документа; внутренний provisioning остаётся помеченным тегом `Internal` и не становится публичным контрактом из-за наличия в OpenAPI.
