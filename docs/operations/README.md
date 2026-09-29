# Эксплуатационный runbook

Runbook описывает текущий контейнерный контур одного округа. Он не заменяет ещё не утверждённые production-политики backup/restore, RPO, RTO, SLA и хранения данных.

## Запуск и остановка локального стека

```powershell
Copy-Item deploy/.env.example deploy/.env
notepad deploy/.env
docker compose --env-file deploy/.env --file deploy/compose.yml config --quiet
docker compose --env-file deploy/.env --file deploy/compose.yml up --build --detach
docker compose --env-file deploy/.env --file deploy/compose.yml ps
```

Порядок запуска фиксирован: healthy PostgreSQL, успешный одноразовый `migrations`, затем API и Worker. API готов принимать трафик только после успешного `/health/ready`.

```powershell
Invoke-RestMethod http://localhost:8080/health/live
Invoke-RestMethod http://localhost:8080/health/ready
docker compose --env-file deploy/.env --file deploy/compose.yml logs --since 10m api worker migrations postgres
```

Остановка без удаления данных:

```powershell
docker compose --env-file deploy/.env --file deploy/compose.yml down
```

`down --volumes` необратимо удаляет локальный named volume PostgreSQL и допустим только для осознанного сброса локальной среды.

## Интерпретация состояния

| Сигнал | Значение | Первичная проверка |
|---|---|---|
| `/health/live` недоступен | API не запущен или не принимает HTTP | Статус контейнера и логи `api`. |
| `/health/live` healthy, `/health/ready` unhealthy | API работает, PostgreSQL недоступна | Логи `postgres`, network/config и строка подключения без вывода секрета. |
| `migrations` завершился с ошибкой | Схема не подготовлена; API/Worker не должны стартовать | Логи `migrations`, доступность БД и совместимость миграции. |
| Worker container unhealthy | PID 1 завершился или не отвечает сигналу процесса | Логи `worker`, последние task outcomes и PostgreSQL connectivity. |
| OTLP отсутствует | Локальная работа продолжается без внешнего telemetry backend | Значение `Observability__OtlpEndpoint` и доступность collector. |

Логи API и Worker являются структурированным JSON. Для связи запроса используйте `X-Correlation-Id`, `CorrelationId` и `TraceId`; не включайте request body, токены, пароли или строки подключения в диагностические сообщения.

## Обновление

Перед production-обновлением обязательны:

1. успешный CI gate для точного commit/tag;
2. утверждённый и проверенный backup/restore для целевого окружения;
3. проверка миграций на копии или staging-базе с сопоставимым объёмом данных;
4. проверка конфигурации и наличия всех секретов без их вывода;
5. применение миграций отдельным job;
6. запуск API и Worker только после успешных миграций;
7. smoke-проверка liveness, readiness и внутреннего контракта в разрешённой среде.

Миграции к чистой PostgreSQL проверяются integration/E2E-тестами. Это не доказывает безопасное время выполнения и обратимость на production-данных.

## Rollback

Schema downgrade не является автоматической стратегией. Возврат предыдущей версии API/Worker допустим только если применённая schema обратно совместима. Для необратимых migrations основной путь — заранее подготовленный forward-fix; восстановление backup используется только по утверждённому incident plan.

Не запускайте `dotnet ef database update <old-migration>` на production без анализа потери данных и утверждённого плана. Отсутствие backup/restore и ответственных за RPO/RTO является блокером production release, а не поводом выбрать значения молча.

## Перед передачей инцидента

Сохраните без секретов:

- commit/tag и версии образов;
- UTC-время начала проблемы;
- correlation/trace ID затронутых запросов;
- результаты `/health/live` и `/health/ready`;
- состояние контейнеров и exit code `migrations`;
- безопасный фрагмент структурированных логов;
- факт последней миграции и изменения конфигурации.

Точный список обязательных настроек находится в [справочнике конфигурации](../configuration/README.md), а ограничения контейнеров — в [руководстве развёртывания](../deployment/README.md).


## Отзыв пользовательских сессий

Director может отозвать все refresh sessions пользователя через:

`POST /api/v1/users/{userId}/sessions/revoke-all`

Операция аудируется и идемпотентна по эффекту: повторный вызов может вернуть `RevokedSessions = 0`. Уже выпущенный access token не хранится server-side и действует до своего короткого expiration; при критичном incident это учитывается в containment window.

## Production PostgreSQL roles

SQL scripts:

- `deploy/postgres/bootstrap-production-roles.sql`;
- `deploy/postgres/grant-runtime.sql`.

После deployment обязательно проверить negative permissions runtime role, а не только успешный health check.

Полный go/no-go список: [production readiness checklist](production-readiness.md).

## Backup / restore drill

Репозиторий содержит два operator helper script:

- `deploy/postgres/backup.ps1` — создаёт custom-format `pg_dump` и выводит SHA256;
- `deploy/postgres/restore-test.ps1` — восстанавливает backup в отдельную базу и проверяет наличие EF migration history.

Пароль передаётся только через `PGPASSWORD` из secret environment оператора.

Успех script не закрывает production readiness автоматически. Нужно также зафиксировать фактические duration, backup timestamp, restore timestamp, размер backup, RPO/RTO result и application smoke validation.
