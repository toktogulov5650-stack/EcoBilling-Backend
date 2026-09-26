# Развёртывание

Каждый округ получает отдельные экземпляры API, Worker и PostgreSQL. Файлы локального контейнерного запуска находятся в `deploy`.

## Worker

Worker требует строку подключения `ConnectionStrings:EcoBilling`. В окружении её следует передавать как секрет, например через `ConnectionStrings__EcoBilling`; строка подключения не хранится в `appsettings.json`.

Общие параметры выполнения находятся в секции `Worker:Execution`:

- `MaxAttempts` — общее максимальное число попыток, не меньше `1`;
- `RetryDelay` — неотрицательная задержка между попытками в формате `TimeSpan`.

Значения по умолчанию проекта — три попытки и пять секунд. Worker корректно передаёт сигнал остановки выполняемому заданию. Автоматическое применение миграций при старте и реальные фоновые задания на этапе ADR-0016 не включены.

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
