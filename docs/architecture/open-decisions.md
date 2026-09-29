# Реестр открытых архитектурных решений

Файл содержит только решения, которые действительно остаются открытыми после реализации v1. Реализованное v1-поведение фиксируется в [ADR-0025](ADR-0025-versioned-product-policy.md) и [политике v1](../business-rules/v1-policy-baseline.md).

## Открытые решения

| Тема | Что ещё требуется определить | До какого этапа |
|---|---|---|
| Имя Resident/сотрудника | Составные поля, окончательная максимальная длина, допустимые символы и локализация | До ужесточения публичного профиля/схемы |
| Формат Account number | Окончательный регламент длины и символов между всеми округами | До межокружного унифицированного импорта |
| Поиск Address | Полнотекстовый/fuzzy поиск, ranking и язык | До отдельного search endpoint |
| Расширенный Meter | Тип, разрядность, пломба, фото и дополнительные статусы | До Meter v2 |
| Billing при замене Meter внутри периода | Формула объединения показаний нескольких Meter и контрольные примеры | До снятия текущего явного запрета |
| Расширенные Reading rules | Максимальный диапазон, фото/доказательства, device/import trust model | До Readings v2 |
| Tariff semantics v2 | Единица ставки и дополнительные категории/льготы | До Tariff/Billing v2 |
| Формула Billing v2+ | Нормативы, минимумы, льготы, пени и перерасчёты | До соответствующей реализации |
| Payment provider | Provider, callback signature, статусы, refunds/reconciliation | До внешней онлайн-оплаты |
| Outbox transport | Broker/transport, delivery semantics, DLQ и publisher | До включения Outbox Worker в production |
| Reports v2 | Фильтры, экспорт, snapshot-time, retention и тяжёлые отчёты | До report exports |
| Initial credential delivery | Безопасный канал передачи initial credential сотрудникам | До внешнего production onboarding |
| Data retention | Audit, readings, payments и персональные данные | Production policy |
| Backup / restore | RPO, RTO, retention, frequency и restore drill | Production readiness |
| Observability | Collector/backend, sampling, dashboards, alerts и retention | Production deployment |
| Deployment security | TLS termination, trusted proxy IP, network policy/firewall | Production deployment |
| Secret management | Provider, RBAC, rotation/revoke, break-glass | Production deployment |
| PostgreSQL production roles | Применение migrator/runtime roles, grants и rotation в целевом окружении | Production deployment |
| Container supply chain | Final image scanning, immutable digest, SBOM/VEX, CVE threshold | Production release |
| Rollout strategy | Replica count, maintenance window, rollback vs forward-fix | Production release |

## Принятые решения v1

- Identity роли, login types, lockout, access/refresh lifecycle и отсутствие публичной регистрации — ADR-0004 и ADR-0025.
- Resident создаётся атомарно вместе с Identity, Address и единственным Account v1.
- Account является источником сохранённой переплаты; debt вычисляется из Charges и PaymentAllocations.
- Controller назначается на Address. Controller получает только свой assignment list/worklist и может вносить Reading только для назначенного ресурса.
- Meter replacement сохраняет историю: старый Meter retired, новый связан через replacement relationship; silent overwrite запрещён.
- MeterReading хранит автора, source и measured time. Обычное значение не уменьшается; backdated/correction требуют полномочий и не переписывают историю.
- Tariff имеет immutable versions с полуоткрытыми непересекающимися периодами; Tariff назначается Account отдельной исторической записью.
- Billing v1 использует календарный месяц `Asia/Bishkek`, accepted readings, effective TariffVersion, KGS и `AwayFromZero` округление до 2 знаков.
- Payment v1 — ручная регистрация подтверждённого платежа Director с обязательной идемпотентностью; allocation идёт по старейшим Charges, остаток становится Account overpayment.
- Worker содержит Monthly Billing и Outbox jobs, bounded retry и PostgreSQL distributed coordination. Production schedules включаются явно.
- Reports v1 предоставляют operational/financial read models.
- AuditLog append-only на уровне приложения; production runtime grant дополнительно запрещает UPDATE/DELETE audit table.
- Forwarded headers не доверяются по умолчанию и могут приниматься только от явно настроенного proxy IP.
- Для production PostgreSQL предусмотрены отдельные migrator/runtime роли; SQL scripts находятся в `deploy/postgres`, но их фактическое применение является deployment evidence, а не свойством репозитория.
