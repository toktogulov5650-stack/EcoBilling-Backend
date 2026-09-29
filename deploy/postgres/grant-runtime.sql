\set ON_ERROR_STOP on

-- Run after EF migrations as a PostgreSQL administrator.
-- The runtime role receives data access but no schema-creation privileges.

GRANT USAGE ON SCHEMA
    identity,
    residents,
    controllers,
    accounts,
    meters,
    readings,
    tariffs,
    billing,
    payments,
    infrastructure
TO ecobilling_runtime;

GRANT SELECT, INSERT, UPDATE, DELETE
ON ALL TABLES IN SCHEMA
    identity,
    residents,
    controllers,
    accounts,
    meters,
    readings,
    tariffs,
    billing,
    payments,
    infrastructure
TO ecobilling_runtime;

GRANT USAGE, SELECT
ON ALL SEQUENCES IN SCHEMA
    identity,
    residents,
    controllers,
    accounts,
    meters,
    readings,
    tariffs,
    billing,
    payments,
    infrastructure
TO ecobilling_runtime;

-- Preserve append-only / security-event boundaries at the database role level.
REVOKE UPDATE, DELETE
ON TABLE infrastructure.audit_logs
FROM ecobilling_runtime;

REVOKE UPDATE, DELETE
ON TABLE infrastructure.internal_service_token_replays
FROM ecobilling_runtime;

REVOKE DELETE
ON TABLE infrastructure.outbox_messages
FROM ecobilling_runtime;

-- Future objects created by EF migrations inherit runtime grants.
ALTER DEFAULT PRIVILEGES FOR ROLE ecobilling_migrator
IN SCHEMA identity, residents, controllers, accounts, meters, readings, tariffs, billing, payments, infrastructure
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO ecobilling_runtime;

ALTER DEFAULT PRIVILEGES FOR ROLE ecobilling_migrator
IN SCHEMA identity, residents, controllers, accounts, meters, readings, tariffs, billing, payments, infrastructure
GRANT USAGE, SELECT ON SEQUENCES TO ecobilling_runtime;
