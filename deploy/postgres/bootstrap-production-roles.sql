\set ON_ERROR_STOP on

-- Run as a PostgreSQL administrator. Pass variables with:
-- psql ... -v database_name=ecobilling -v migrator_password='...' -v runtime_password='...' -f bootstrap-production-roles.sql

SELECT 'CREATE ROLE ecobilling_migrator LOGIN'
WHERE NOT EXISTS (
    SELECT 1 FROM pg_roles WHERE rolname = 'ecobilling_migrator'
) \gexec

SELECT 'CREATE ROLE ecobilling_runtime LOGIN'
WHERE NOT EXISTS (
    SELECT 1 FROM pg_roles WHERE rolname = 'ecobilling_runtime'
) \gexec

SELECT format(
    'ALTER ROLE ecobilling_migrator WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS',
    :'migrator_password') \gexec

SELECT format(
    'ALTER ROLE ecobilling_runtime WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOREPLICATION NOBYPASSRLS',
    :'runtime_password') \gexec

SELECT format(
    'GRANT CONNECT ON DATABASE %I TO ecobilling_migrator',
    :'database_name') \gexec

SELECT format(
    'GRANT CONNECT ON DATABASE %I TO ecobilling_runtime',
    :'database_name') \gexec

SELECT format(
    'GRANT CREATE ON DATABASE %I TO ecobilling_migrator',
    :'database_name') \gexec
