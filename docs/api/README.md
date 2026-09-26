# API

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

Заголовок `Idempotency-Replayed` равен `false` для создания и `true` для успешного повтора. Повтор должен использовать новый JWT и прежний `Idempotency-Key`.

JWT принимается только с RS256, известным `kid`, настроенными issuer/audience, уникальным `jti`, коротким сроком действия и scope `ecobilling.directors.provision`. Каждый `jti` принимается только один раз.

Начальная учётная тайна должна содержать 32–256 символов без пробельных символов. Она хешируется и не возвращается. Директор остаётся в состоянии обязательной первичной установки пароля; обычный вход до её завершения запрещён.

Ошибки используют `application/problem+json` с расширениями `code`, `traceId` и, для ошибок проверки, `validationErrors`. Основные коды:

- `service.unauthorized` — JWT отсутствует, неверен или уже использован;
- `operation.invalid_idempotency_key` — заголовок отсутствует или некорректен;
- `operation.idempotency_conflict` — ключ уже связан с другим запросом;
- `director.already_exists` — директор или email уже существует;
- `director.invalid_full_name`, `auth.invalid_login`, `director.invalid_initial_credential` — неверные поля запроса.

Открытая начальная тайна, JWT, закрытый ключ Control и HMAC-ключ fingerprint не должны попадать в логи, telemetry или committed configuration.
