# Безопасность и готовность к выпуску

Актуализировано 29 сентября 2026 года после завершения backend v1 и production-readiness hardening. Проверка охватывает исходный код, конфигурацию, зависимости, CI, контейнерные манифесты, миграции и автоматизированные тесты.

## Решение о выпуске

Backend подходит для development и staging. Для production статус остаётся **NO-GO до закрытия environment-specific блокеров**.

Причина NO-GO — не отсутствие основных бизнес-сценариев. Основной v1 backend реализован и проходит CI. Блокеры относятся к реальному production окружению, внешним интеграциям и эксплуатационным доказательствам.

## Реализованные защитные меры

- NuGet audit включён для прямых и транзитивных зависимостей.
- GitHub Actions используют SHA-pinned actions, read-only permissions, Release build/test, настоящую PostgreSQL, Compose validation, container build и Trivy image scanning.
- Публичной регистрации нет.
- User authentication использует короткоживущий access JWT, hash-only refresh storage, rotation, token-family revoke при replay и persisted lockout.
- Authentication endpoints защищены rate limiting.
- Director может отозвать все refresh sessions конкретного пользователя через отдельный role-protected endpoint; операция аудируется.
- Internal provisioning использует отдельный RS256 JWT contract с issuer/audience, `kid`, scope и `jti` replay protection.
- User JWT signing keys поддерживают `kid` и безопасную ротацию.
- `AllowedHosts` не допускает wildcard в deployment.
- Forwarded headers по умолчанию не доверяются. Их обработка включается только при явном `ReverseProxy:Enabled=true` и списке точных trusted proxy IP.
- Problem Details не возвращает exception detail, connection strings, tokens или credentials.
- API/Worker не включают sensitive-data logging.
- Mutation-сценарии записывают AuditLog атомарно с бизнес-изменением.
- Runtime-контейнеры non-root, read-only, без Linux capabilities и с `no-new-privileges`.
- Миграции вынесены в отдельный container target.
- В `deploy/postgres` есть воспроизводимые SQL scripts для разделения `ecobilling_migrator` и `ecobilling_runtime` и применения runtime grants.

## Последний подтверждённый CI verification

На текущем head production-readiness ветки `9c93eafc3f1fe1329f368b8885beeeadd30b9b7c` GitHub Actions CI run 73 завершился успешно: Architecture 34/34, Unit 307/307, PostgreSQL Integration 206/206, E2E 4/4. Docker Compose validation и сборка `api`, `worker`, `migrations`, `postgres` прошли успешно. Trivy scan с `CRITICAL,HIGH` и `ignore-unfixed=true` показал 0 findings для всех четырёх финальных images.

## Production блокеры

| Блокер | Что требуется перед GO |
|---|---|
| Secret store | Выбрать реальное хранилище секретов, RBAC, rotation/revoke процедуру и аварийный доступ. Не хранить production secrets в `.env`, Git или image layers. |
| PostgreSQL credentials | Применить отдельные migrator/runtime роли в целевой БД, хранить их secrets отдельно и проверить deployment с минимальными grants. |
| TLS / ingress | Настроить TLS termination, exact `AllowedHosts`, trusted proxy IP и network policy/firewall. |
| Backup / restore | Утвердить RPO/RTO/retention, выполнить backup и доказуемый restore test на отдельном окружении. |
| Observability | Подключить OTLP collector/backend, определить sampling, dashboards, alert thresholds и recipients. |
| Container supply chain | CI уже блокирует CRITICAL/HIGH findings Trivy для `api`, `worker`, `migrations`, `postgres`; перед GO ещё нужно фиксировать immutable digest/SBOM и утвердить lifecycle/risk-acceptance policy. |
| Migration rehearsal | Прогнать migrations на копии БД с реалистичным объёмом, измерить lock/downtime и проверить forward-fix/rollback procedure. |
| Temporary staff credentials | Утвердить безопасный канал передачи initial credential Director/Controller; API не должен отправлять секрет через обычный response/log. |
| External payment provider | Если нужна онлайн-оплата: provider callback, signature validation, replay protection, refunds и reconciliation. |
| External Outbox transport | Выбрать broker/transport и зарегистрировать реальный `IOutboxMessagePublisher`; Outbox Worker нельзя включать без publisher. |

## Остаточные продуктовые ограничения v1

- Billing через период, в котором участвует несколько Meter после замены, намеренно блокируется до утверждения формулы.
- Льготы, нормативы, пени и сложные перерасчёты не симулируются.
- Payment provider workflow отсутствует; поддерживается только ручная регистрация уже подтверждённого платежа Director.
- Monthly Billing и Outbox schedules в Compose по умолчанию выключены и включаются оператором после проверки зависимостей.

## Воспроизводимая проверка

```powershell
dotnet restore EcoBilling.slnx -warnaserror -p:NuGetAudit=true -p:NuGetAuditMode=all
dotnet package list --project EcoBilling.slnx --include-transitive --vulnerable --no-restore
dotnet build EcoBilling.slnx --configuration Release --no-restore
dotnet test EcoBilling.slnx --configuration Release --no-build
docker compose --env-file deploy/.env.example --file deploy/compose.yml config --quiet
docker compose --env-file deploy/.env.example --file deploy/compose.yml build api worker migrations postgres
```

GO для production возможен только после закрытия применимых блокеров с проверяемыми evidence для конкретного окружения. Код не должен объявлять внешнюю инфраструктуру «готовой» только потому, что локальный Compose запускается.
