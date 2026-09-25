# Архитектура EcoBilling

EcoBilling — модульный монолит одного округа с отдельными API, Worker и PostgreSQL.

## Архитектурные решения

- [ADR-0001: границы модульного монолита](ADR-0001-modular-monolith-boundaries.md)
- [ADR-0002: принадлежность экземпляра одному округу](ADR-0002-district-instance-ownership.md)
- [ADR-0003: формат HTTP-ошибок RFC 7807](ADR-0003-rfc7807-error-contract.md)
- [ADR-0004: граница Identity и идентификаторы входа](ADR-0004-identity-boundary-and-login-identifiers.md)
- [ADR-0005: persistence Identity и хеширование паролей](ADR-0005-identity-persistence-and-password-hashing.md)
- [ADR-0006: граница профиля Resident](ADR-0006-resident-profile-boundary.md)
- [Реестр открытых решений](open-decisions.md)

Фактические `ProjectReference` проверяются проектом `EcoBilling.ArchitectureTests` непосредственно по `EcoBilling.slnx` и файлам проектов.
