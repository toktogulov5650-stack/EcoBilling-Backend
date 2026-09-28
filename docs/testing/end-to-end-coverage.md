# Матрица End-to-End покрытия

E2E-тесты выполняют пользовательский или межсервисный путь через HTTP и настоящую PostgreSQL. Они не создают HTTP-контракты для функций, которых пока нет в production API.

| Сценарий из задания | Состояние | Проверка или причина ожидания |
|---|---|---|
| Вход жителя | Покрыто | E2E создаёт Resident директором и проверяет вход по нормализованному номеру лицевого счёта. |
| Вход контроллера | Покрыто | E2E создаёт Controller, проверяет запрет login до password setup, замену тайны и успешный login. |
| Вход директора | Покрыто | E2E provisioning завершается password setup и успешным login Director. |
| Отказ при неверном пароле | Покрыто integration | HTTP/PostgreSQL тесты проверяют единый `401 auth.invalid_credentials` и persisted lockout. |
| Запрет публичной регистрации | Покрыто | Endpoint inventory не содержит registration routes; HTTP-запрос регистрации получает RFC 7807 `404` с `request.not_found`. |
| Создание жителя директором | Покрыто | Полный HTTP/PostgreSQL journey проверяет Director JWT, атомарное создание Identity/Profile/Address/Account, audit, replay и вход Resident. |
| Сброс пароля жителя директором | Покрыто | E2E проверяет идемпотентный reset, отзыв старого refresh token, запрет старого пароля, вход с новым паролем и Audit без credentials. |
| Создание контроллера директором | Покрыто | Полный HTTP/PostgreSQL journey проверяет Director JWT, атомарное создание, audit, replay и первичную смену пароля Controller. |
| Житель не видит чужие данные | Ожидает auth/API | Нет пользовательских claims и HTTP endpoint просмотра счёта. |
| Контроллер не видит неназначенного жителя | Ожидает назначения | Модель назначений и соответствующий HTTP API не утверждены. |
| Внесение показания контроллером | Ожидает правила/API | Нет mutation-сценария; правила показаний остаются открытыми. |
| Просмотр истории показаний | Ожидает API | Есть persistence-основа, но нет пользовательского query endpoint. |
| Просмотр начислений | Ожидает правила/API | Нет утверждённой формулы и пользовательского query endpoint. |
| Просмотр платежей | Ожидает API | Есть минимальный подтверждённый Payment, но нет пользовательского query endpoint. |
| Внутреннее создание директора | Покрыто | Проверяются отдельная service authentication, полный HTTP/persistence путь, audit и отсутствие утечки initial credential. |
| Идемпотентный повтор внутренней операции | Покрыто | Новый JWT с тем же `Idempotency-Key` возвращает исходные идентификаторы и не создаёт дубликаты. |

## Запуск

Укажите административную строку подключения к PostgreSQL, пользователь которой может создавать и удалять временные базы:

```powershell
$env:ECOBILLING_TEST_POSTGRES_CONNECTION = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=<local-test-password>"
dotnet test tests/EcoBilling.EndToEndTests/EcoBilling.EndToEndTests.csproj --configuration Release
```

GitHub Actions задаёт эту переменную автоматически для одноразового PostgreSQL service. Production credentials для E2E не используются.
