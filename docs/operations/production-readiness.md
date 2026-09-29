# Production readiness checklist

Этот checklist отделяет готовность backend-кода от готовности конкретного production-окружения. Пункт считается закрытым только при наличии проверяемого evidence: config/change record, test result, restore log, scan report или approval.

## 1. Release artifact

- [ ] Выбран точный commit/tag.
- [ ] CI для commit зелёный.
- [ ] Container images собраны один раз и зафиксированы immutable digest.
- [ ] NuGet audit выполнен.
- [ ] Финальные images просканированы выбранным scanner.
- [ ] SBOM сохранён вместе с release metadata.
- [ ] Critical/High findings закрыты или имеют документированный risk acceptance.

## 2. Secrets

Production secrets не должны храниться в Git, `.env`, image layers или issue/CI output.

Обязательные secret values:

- runtime PostgreSQL connection;
- migration PostgreSQL connection;
- user JWT signing key;
- Director provisioning fingerprint key;
- private credentials внешних integrations, если они подключены.

Для каждого секрета должны быть определены owner, secret store, access policy, rotation period, revoke procedure и break-glass procedure.

## 3. PostgreSQL roles

Рекомендуемая модель:

- `ecobilling_migrator` — schema migrations, не используется API/Worker;
- `ecobilling_runtime` — API/Worker, без CREATE/ALTER schema;
- отдельный administrator — только bootstrap/операционные действия.

В репозитории есть `deploy/postgres/bootstrap-production-roles.sql`, `deploy/postgres/grant-runtime.sql` и `deploy/postgres/verify-production-roles.sql`.

Пример bootstrap из защищённой operator shell:

```powershell
psql "<admin-connection>" `
  -v database_name=ecobilling `
  -v migrator_password="$env:ECOBILLING_MIGRATOR_PASSWORD" `
  -v runtime_password="$env:ECOBILLING_RUNTIME_PASSWORD" `
  -f deploy/postgres/bootstrap-production-roles.sql
```

После EF migrations:

```powershell
psql "<admin-connection>" -f deploy/postgres/grant-runtime.sql
```

После grants выполните `deploy/postgres/verify-production-roles.sql`. Дополнительно проверьте runtime connection реальным smoke-запросом API/Worker: runtime не меняет schema, не может UPDATE/DELETE `infrastructure.audit_logs`, migrator применяет migrations, а API/Worker не используют migrator credential.

## 4. TLS / ingress / proxy

- [ ] TLS завершается на утверждённом ingress/reverse proxy.
- [ ] `AllowedHosts` содержит только ожидаемые hosts.
- [ ] `ReverseProxy__Enabled=true` только за реальным proxy.
- [ ] `ReverseProxy__KnownProxies__0` содержит точный IP доверенного hop.
- [ ] `ForwardLimit` соответствует фактической proxy chain.
- [ ] PostgreSQL не опубликована в public network.
- [ ] Firewall/network policy разрешает только нужные связи.

EcoBilling не доверяет `X-Forwarded-For` / `X-Forwarded-Proto` по умолчанию.

## 5. Backup / restore

До production должны быть утверждены RPO, RTO, backup frequency, retention, encryption/location и owner/on-call.

Минимальное evidence — успешный restore backup в отдельную PostgreSQL и запуск read-only/smoke validation против восстановленной копии. Backup без проверенного restore не считается закрытым пунктом.

## 6. Migration rehearsal

На sanitized копии с реалистичным объёмом:

1. снять backup;
2. применить тот же migration image/commit;
3. измерить duration и locks;
4. запустить smoke queries и application readiness;
5. проверить совместимость предыдущего API/Worker, если rollback бинарников допускается;
6. зафиксировать forward-fix plan для необратимой schema migration.

Не выполнять production downgrade `dotnet ef database update <old>` без отдельного анализа потери данных.

## 7. Observability

- [ ] Настроен `Observability__OtlpEndpoint`.
- [ ] Collector принимает traces/metrics.
- [ ] Логи централизованы.
- [ ] Есть dashboard API availability/latency/errors.
- [ ] Есть dashboard Worker outcomes/duration.
- [ ] Alerts настроены на readiness, error rate, PostgreSQL availability, failed migrations, Worker failures и exhausted Outbox retries.
- [ ] Назначены recipients/escalation.

## 8. Worker enablement

По умолчанию schedules выключены.

Monthly Billing можно включать только после проверки v1 billing policy на staging data, migration rehearsal и alerting.

Outbox Worker можно включать только когда зарегистрирован реальный `IOutboxMessagePublisher` для ожидаемых message types.

## 9. External payment integration

Ручные подтверждённые Payments v1 работают без provider. Если включается online provider, до GO нужны callback authentication/signature, replay/idempotency, status mapping, refund/cancel workflow, reconciliation, audit и regression tests.

## 10. Go / no-go

GO фиксируется только после закрытия всех применимых пунктов и повторного:

```powershell
dotnet restore EcoBilling.slnx -warnaserror
dotnet build EcoBilling.slnx --configuration Release --no-restore
dotnet test EcoBilling.slnx --configuration Release --no-build
docker compose --env-file deploy/.env.example --file deploy/compose.yml config --quiet
docker compose --env-file deploy/.env.example --file deploy/compose.yml build api worker migrations
```

Связанные документы: [Security readiness](../security/README.md), [Deployment](../deployment/README.md), [Operations runbook](README.md), [Configuration](../configuration/README.md).
