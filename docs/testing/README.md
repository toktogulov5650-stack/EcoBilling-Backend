# Тестирование

- Unit — бизнес-правила без инфраструктуры.
- Unit также проверяет инфраструктурно-независимый runner Worker: успешное выполнение, ограниченные повторы, исчерпание попыток и отмену.
- Integration — база данных и интеграции.
- Contract/integration проверки внутреннего API запускают настоящий ASP.NET Core pipeline и PostgreSQL: RS256 JWT, обязательный `kid` и scope, одноразовый `jti`, RFC 7807 и идемпотентный повтор.
- Integration-тесты Audit/Outbox проверяют миграцию на настоящей PostgreSQL, JSONB, UTC-время, append-only защиту аудита, переходы Outbox и отсутствие дубликата аудита при replay/concurrency.
- Integration-тесты observability проверяют независимость liveness от PostgreSQL, readiness failure без утечки строки подключения, correlation ID и валидацию OTLP endpoint.
- Unit-тесты Worker дополнительно проверяют activity и безопасные outcome tags фонового задания.
- Architecture — направления зависимостей.
- Architecture также проверяет Dockerfile/Compose: non-root runtime, health checks, обязательные секреты, правильный ключ connection string и порядок PostgreSQL → migrations → приложения.
- EndToEnd — сценарии через HTTP API.
