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
- [Реестр открытых решений](open-decisions.md)

Фактические `ProjectReference` проверяются проектом `EcoBilling.ArchitectureTests` непосредственно по `EcoBilling.slnx` и файлам проектов.
