# Архитектура EcoBilling

EcoBilling — модульный монолит одного округа с отдельными API, Worker и PostgreSQL.

## Архитектурные решения

- [ADR-0001: границы модульного монолита](ADR-0001-modular-monolith-boundaries.md)
- [ADR-0002: принадлежность экземпляра одному округу](ADR-0002-district-instance-ownership.md)
- [ADR-0003: формат HTTP-ошибок RFC 7807](ADR-0003-rfc7807-error-contract.md)
- [ADR-0004: граница Identity и идентификаторы входа](ADR-0004-identity-boundary-and-login-identifiers.md)
- [ADR-0005: persistence Identity и хеширование паролей](ADR-0005-identity-persistence-and-password-hashing.md)
- [ADR-0006: граница профиля Resident](ADR-0006-resident-profile-boundary.md)
- [ADR-0007: граница профиля Controller](ADR-0007-controller-profile-boundary.md)
- [ADR-0008: граница лицевого счёта](ADR-0008-account-boundary.md)
- [ADR-0009: адрес обслуживаемого объекта](ADR-0009-address-boundary.md)
- [ADR-0010: базовая граница Meter](ADR-0010-meter-boundary.md)
- [ADR-0011: базовая граница MeterReading](ADR-0011-reading-boundary.md)
- [ADR-0012: Tariff и неизменяемые версии ставок](ADR-0012-tariff-versioning-boundary.md)
- [ADR-0013: минимальная граница Charge без формулы расчёта](ADR-0013-billing-charge-foundation.md)
- [ADR-0014: подтверждённый Payment без провайдерского workflow](ADR-0014-confirmed-payment-foundation.md)
- [ADR-0015: Reports как read-only проекция существующих данных](ADR-0015-reports-read-model-boundary.md)
- [ADR-0016: граница выполнения фоновых заданий Worker](ADR-0016-worker-execution-boundary.md)
- [ADR-0017: внутренний контракт provisioning директора](ADR-0017-control-director-provisioning-contract.md)
- [ADR-0018: аудит и инфраструктурная основа Outbox](ADR-0018-audit-and-outbox-foundation.md)
- [ADR-0019: фундамент наблюдаемости](ADR-0019-observability-foundation.md)
- [ADR-0020: контейнерное развёртывание одного округа](ADR-0020-container-deployment-boundary.md)
- [ADR-0021: обязательный CI-gate](ADR-0021-continuous-integration-gate.md)
- [ADR-0022: граница End-to-End тестов](ADR-0022-end-to-end-test-boundary.md)
- [ADR-0023: финальная документационная основа](ADR-0023-final-documentation-baseline.md)
- [ADR-0024: финальная проверка безопасности и готовности](ADR-0024-final-security-readiness-review.md)
- [ADR-0025: версионируемая политика продукта](ADR-0025-versioned-product-policy.md)
- [ADR-0026: создание Resident и кардинальность Account в v1](ADR-0026-resident-account-v1-cardinality.md)
- [ADR-0027: сброс пароля Resident директором](ADR-0027-resident-password-reset.md)
- [Реестр открытых решений](open-decisions.md)

Фактические `ProjectReference` проверяются проектом `EcoBilling.ArchitectureTests` непосредственно по `EcoBilling.slnx` и файлам проектов.
