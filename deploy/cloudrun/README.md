# Cloud Run + Neon

Этот контур разворачивает API как Cloud Run service, миграции как отдельный Cloud Run
job и каждую фоновую работу как завершающийся Cloud Run job. Долгоживущий режим Worker
остаётся только для Docker Compose. Cloud Run Scheduler должен запускать jobs по
утверждённому расписанию; расписание начислений здесь намеренно не задаётся.

## 1. Neon

Создайте отдельную production branch/database и две роли:

- migration role — владелец схемы и объектов, используется только migration job;
- runtime role — получает только необходимые права на уже созданные схемы и таблицы.

Для `ECOBILLING_DESIGN_TIME_CONNECTION` используйте **direct** Neon endpoint. Для
`ConnectionStrings__EcoBilling` используйте endpoint с `-pooler` в имени host. Обе строки
должны проверять сертификат, например:

```text
Host=ep-example.eu-central-1.aws.neon.tech;Database=neondb;Username=ecobilling_migrator;Password=<secret>;SSL Mode=VerifyFull;Trust Server Certificate=false;Timeout=15;Command Timeout=60
Host=ep-example-pooler.eu-central-1.aws.neon.tech;Database=neondb;Username=ecobilling_runtime;Password=<secret>;SSL Mode=VerifyFull;Trust Server Certificate=false;Maximum Pool Size=20;Connection Idle Lifetime=60;Timeout=15;Command Timeout=30
```

Не используйте production branch для integration tests: тестовый harness создаёт и
удаляет базы. Перед первым release убедитесь, что migration role может выполнить
`CREATE EXTENSION IF NOT EXISTS btree_gist`.

Сохраните строки отдельно в Secret Manager, например как
`ecobilling-neon-migrations` и `ecobilling-neon-runtime`. Секреты JWT, fingerprint key и
публичный PEM также должны быть отдельными secrets/config values. Не передавайте их как
build substitutions.

## 2. Образы

Создайте Artifact Registry repository один раз, затем соберите оба образа. Используйте
immutable tag (commit SHA или release tag), не `latest`, для production release.

```powershell
gcloud artifacts repositories create ecobilling --repository-format=docker --location=europe-west1
gcloud builds submit --config deploy/cloudrun/cloudbuild.yaml --substitutions=_REGION=europe-west1,_REPOSITORY=ecobilling,_TAG=<commit-sha> .
```

Cloud Build отдельно публикует target `migrations` из API Dockerfile. Сначала создайте job, затем
явно выполните его и дождитесь успеха:

```powershell
gcloud run jobs deploy ecobilling-migrations --region=europe-west1 --image=europe-west1-docker.pkg.dev/<project>/ecobilling/ecobilling-migrations:<commit-sha> --set-secrets=ECOBILLING_DESIGN_TIME_CONNECTION=ecobilling-neon-migrations:1 --tasks=1 --max-retries=0 --task-timeout=10m
gcloud run jobs execute ecobilling-migrations --region=europe-west1 --wait
```

API нельзя обновлять до успешного завершения migration job.

## 3. API service

Deploy должен передать все обязательные ключи из
`docs/configuration/README.md`. Ниже показан каркас; значения issuer, audience, key id и
`AllowedHosts` являются конфигурацией конкретного округа. Для PEM удобнее secret volume,
если выбранный deployment pipeline не сохраняет переводы строк в environment secret.

```powershell
gcloud run deploy ecobilling-api --region=europe-west1 --image=europe-west1-docker.pkg.dev/<project>/ecobilling/ecobilling-api:<commit-sha> --service-account=ecobilling-api@<project>.iam.gserviceaccount.com --port=8080 --cpu=1 --memory=512Mi --concurrency=40 --min=0 --max=5 --timeout=30s --set-env-vars=ASPNETCORE_ENVIRONMENT=Production,AllowedHosts=<api-host>,InternalServiceAuthentication__Issuer=<control-issuer>,InternalServiceAuthentication__Audience=<district-audience>,InternalServiceAuthentication__SigningKeys__0__KeyId=<control-key-id>,UserAuthentication__Issuer=<api-issuer>,UserAuthentication__Audience=<user-audience>,UserAuthentication__ActiveSigningKeyId=<user-key-id>,UserAuthentication__SigningKeys__0__KeyId=<user-key-id> --set-secrets=ConnectionStrings__EcoBilling=ecobilling-neon-runtime:1,DirectorProvisioning__RequestFingerprintKey=ecobilling-fingerprint:1,InternalServiceAuthentication__SigningKeys__0__PublicKeyPem=ecobilling-control-public-key:1,UserAuthentication__SigningKeys__0__SecretBase64=ecobilling-user-signing-key:1
```

Cloud Run по умолчанию защищает service через IAM. Решение о публичном ingress и
`--allow-unauthenticated` принимается отдельно: JWT приложения не заменяет сетевую/IAM
политику. После deploy проверьте, что `AllowedHosts` содержит точный `run.app` или custom
domain host. Не используйте `*`.

Настройте startup/liveness HTTP probes на `/health/live`; `/health/ready` проверяет Neon
и предназначен для smoke/monitoring. Ограничьте `max instances` совместно с
`Maximum Pool Size`, чтобы всплеск scale-out не исчерпал соединения.

## 4. Worker jobs

Один image поддерживает две завершающиеся команды:

```powershell
gcloud run jobs deploy ecobilling-monthly-billing --region=europe-west1 --image=europe-west1-docker.pkg.dev/<project>/ecobilling/ecobilling-worker:<commit-sha> --args=--task=monthly-billing --set-secrets=ConnectionStrings__EcoBilling=ecobilling-neon-runtime:1 --tasks=1 --parallelism=1 --max-retries=0 --task-timeout=30m
gcloud run jobs deploy ecobilling-outbox --region=europe-west1 --image=europe-west1-docker.pkg.dev/<project>/ecobilling/ecobilling-worker:<commit-sha> --args=--task=outbox --set-secrets=ConnectionStrings__EcoBilling=ecobilling-neon-runtime:1 --set-env-vars=Worker__Schedules__OutboxBatchSize=100 --tasks=1 --parallelism=1 --max-retries=0 --task-timeout=10m
```

Распределённая PostgreSQL-блокировка защищает от перекрывающихся запусков. Повторные
попытки уже есть внутри Worker; поэтому platform retries оставлены равными нулю, чтобы
не умножать попытки неожиданно. Cloud Scheduler создавайте только после утверждения
расписаний и выдайте его service account минимальное право запуска конкретного job.

## 5. Release gate

1. `dotnet build EcoBilling.slnx` и `dotnet test EcoBilling.slnx` успешны.
2. Образы собраны с immutable tag и прошли scan.
3. Есть проверенное восстановление Neon и зафиксированы RPO/RTO.
4. Migration job успешно завершён на staging branch, затем на production branch.
5. API revision получает трафик только после `/health/live` и `/health/ready`.
6. Выполнены login smoke-test и один разрешённый read-only API сценарий.
7. Проверены Cloud Logging alerts для 5xx, failed jobs и database readiness.

Rollback приложения допустим только при обратной совместимости уже применённой схемы.
Автоматический rollback миграций не выполняется.
