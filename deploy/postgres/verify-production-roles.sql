\set ON_ERROR_STOP on

-- Run as a PostgreSQL administrator after bootstrap-production-roles.sql,
-- EF migrations and grant-runtime.sql. This script fails when the production
-- role boundary is broader than the documented EcoBilling model.

DO $$
DECLARE
    role_record record;
BEGIN
    FOR role_record IN
        SELECT rolname, rolsuper, rolcreatedb, rolcreaterole, rolreplication, rolbypassrls
        FROM pg_roles
        WHERE rolname IN ('ecobilling_migrator', 'ecobilling_runtime')
    LOOP
        IF role_record.rolsuper
           OR role_record.rolcreatedb
           OR role_record.rolcreaterole
           OR role_record.rolreplication
           OR role_record.rolbypassrls THEN
            RAISE EXCEPTION
                'Role % has prohibited cluster-level privileges.',
                role_record.rolname;
        END IF;
    END LOOP;

    IF NOT EXISTS (
        SELECT 1 FROM pg_roles WHERE rolname = 'ecobilling_migrator'
    ) THEN
        RAISE EXCEPTION 'Role ecobilling_migrator does not exist.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM pg_roles WHERE rolname = 'ecobilling_runtime'
    ) THEN
        RAISE EXCEPTION 'Role ecobilling_runtime does not exist.';
    END IF;

    IF has_table_privilege(
        'ecobilling_runtime',
        'infrastructure.audit_logs',
        'UPDATE')
       OR has_table_privilege(
        'ecobilling_runtime',
        'infrastructure.audit_logs',
        'DELETE') THEN
        RAISE EXCEPTION
            'ecobilling_runtime must not UPDATE or DELETE infrastructure.audit_logs.';
    END IF;

    IF has_table_privilege(
        'ecobilling_runtime',
        'infrastructure.internal_service_token_replays',
        'UPDATE')
       OR has_table_privilege(
        'ecobilling_runtime',
        'infrastructure.internal_service_token_replays',
        'DELETE') THEN
        RAISE EXCEPTION
            'ecobilling_runtime must not mutate internal service replay records.';
    END IF;

    IF has_table_privilege(
        'ecobilling_runtime',
        'infrastructure.outbox_messages',
        'DELETE') THEN
        RAISE EXCEPTION
            'ecobilling_runtime must not DELETE infrastructure.outbox_messages.';
    END IF;
END
$$;

SELECT
    rolname,
    rolsuper,
    rolcreatedb,
    rolcreaterole,
    rolreplication,
    rolbypassrls
FROM pg_roles
WHERE rolname IN ('ecobilling_migrator', 'ecobilling_runtime')
ORDER BY rolname;
