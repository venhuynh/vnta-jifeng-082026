#!/usr/bin/env bash
set -euo pipefail

# Resets all HRM/ADMS application data while preserving ASP.NET Core Identity
# and EF Core migration history. Run this only during a maintenance window,
# after both hrm-web and adms-gateway have been stopped.

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="${1:-/opt/vnta/shared/env/.env.production}"
CONFIRMATION="${2:-}"

if [[ "$CONFIRMATION" != "--confirm-reset" ]]; then
  cat >&2 <<'USAGE'
Usage:
  ./reset-hrm-business-data.sh [env-file] --confirm-reset

This permanently removes all application rows in the public and audit schemas,
except ASP.NET Core Identity data and __EFMigrationsHistory. A database backup
is created first. Stop hrm-web and adms-gateway before running this command.
USAGE
  exit 64
fi

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Không tìm thấy env file: $ENV_FILE" >&2
  exit 1
fi

set -a
# shellcheck disable=SC1090
. "$ENV_FILE"
set +a

: "${DATABASE_HOST:?set DATABASE_HOST}"
: "${DATABASE_PORT:?set DATABASE_PORT}"
: "${DATABASE_NAME:?set DATABASE_NAME}"
: "${DATABASE_USERNAME:?set DATABASE_USERNAME}"
: "${DATABASE_PASSWORD:?set DATABASE_PASSWORD}"

BACKUP_DIR="${BACKUP_DIR:-/opt/vnta/shared/backups}"
RESET_TIMESTAMP="$(date +%Y%m%d-%H%M%S)"
RESET_BACKUP_NAME="${COMPOSE_PROJECT_NAME:-vnta}-before-business-data-reset-${RESET_TIMESTAMP}.dump"
EXPECTED_DATABASE_NAME="jifeng_hrm"

if [[ "$DATABASE_NAME" != "$EXPECTED_DATABASE_NAME" ]]; then
  echo "Refusing reset: DATABASE_NAME must be '$EXPECTED_DATABASE_NAME'." >&2
  exit 1
fi

echo "Tạo backup trước khi reset database '$DATABASE_NAME'..."
BACKUP_NAME="$RESET_BACKUP_NAME" "$SCRIPT_DIR/backup-db.sh" "$ENV_FILE"

echo "Kiểm tra archive backup '$RESET_BACKUP_NAME'..."
docker run --rm \
  -v "$BACKUP_DIR:/backup:ro" \
  postgres:16-alpine \
  pg_restore --list "/backup/$RESET_BACKUP_NAME" >/dev/null

echo "Reset dữ liệu nghiệp vụ trong database '$DATABASE_NAME'..."
docker run --rm -i \
  -e PGPASSWORD="$DATABASE_PASSWORD" \
  postgres:16-alpine \
  psql \
    --set ON_ERROR_STOP=1 \
    --host "$DATABASE_HOST" \
    --port "$DATABASE_PORT" \
    --username "$DATABASE_USERNAME" \
    --dbname "$DATABASE_NAME" <<'SQL'
BEGIN;

SET LOCAL search_path = public, pg_catalog;

DO $guard$
DECLARE
    unexpected_fk text;
BEGIN
    IF to_regclass('public."AspNetUsers"') IS NULL
       OR to_regclass('public."__EFMigrationsHistory"') IS NULL
       OR to_regclass('public.employees') IS NULL THEN
        RAISE EXCEPTION
            'Refusing reset: the database is missing the expected HRM Identity or employees tables.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'AspNetUsers'
          AND column_name = 'EmployeeId') THEN
        RAISE EXCEPTION
            'Refusing reset: public."AspNetUsers"."EmployeeId" is missing.';
    END IF;

    -- Retained Identity tables must not reference a table that will be
    -- truncated. The known AspNetUsers -> employees FK is handled below.
    SELECT format(
               '%I.%I constraint %I references %I.%I',
               source_namespace.nspname,
               source_table.relname,
               constraint_row.conname,
               target_namespace.nspname,
               target_table.relname)
    INTO unexpected_fk
    FROM pg_catalog.pg_constraint AS constraint_row
    INNER JOIN pg_catalog.pg_class AS source_table
        ON source_table.oid = constraint_row.conrelid
    INNER JOIN pg_catalog.pg_namespace AS source_namespace
        ON source_namespace.oid = source_table.relnamespace
    INNER JOIN pg_catalog.pg_class AS target_table
        ON target_table.oid = constraint_row.confrelid
    INNER JOIN pg_catalog.pg_namespace AS target_namespace
        ON target_namespace.oid = target_table.relnamespace
    WHERE constraint_row.contype = 'f'
      AND source_namespace.nspname = 'public'
      AND lower(source_table.relname) LIKE 'aspnet%'
      AND target_namespace.nspname IN ('public', 'audit')
      AND NOT (
          source_table.relname = 'AspNetUsers'
          AND target_namespace.nspname = 'public'
          AND target_table.relname = 'employees')
    LIMIT 1;

    IF unexpected_fk IS NOT NULL THEN
        RAISE EXCEPTION
            'Refusing reset: retained Identity has an unexpected business-data FK: %.',
            unexpected_fk;
    END IF;

    -- Do not rely on TRUNCATE to discover a reference from an extension,
    -- another schema, or other retained table. Fail before changing data.
    SELECT format(
               '%I.%I constraint %I references %I.%I',
               source_namespace.nspname,
               source_table.relname,
               constraint_row.conname,
               target_namespace.nspname,
               target_table.relname)
    INTO unexpected_fk
    FROM pg_catalog.pg_constraint AS constraint_row
    INNER JOIN pg_catalog.pg_class AS source_table
        ON source_table.oid = constraint_row.conrelid
    INNER JOIN pg_catalog.pg_namespace AS source_namespace
        ON source_namespace.oid = source_table.relnamespace
    INNER JOIN pg_catalog.pg_class AS target_table
        ON target_table.oid = constraint_row.confrelid
    INNER JOIN pg_catalog.pg_namespace AS target_namespace
        ON target_namespace.oid = target_table.relnamespace
    WHERE constraint_row.contype = 'f'
      AND target_table.relkind IN ('r', 'p')
      AND target_namespace.nspname IN ('public', 'audit')
      AND NOT (
          target_namespace.nspname = 'public'
          AND (
              lower(target_table.relname) LIKE 'aspnet%'
              OR target_table.relname = '__EFMigrationsHistory'
          )
      )
      AND NOT EXISTS (
          SELECT 1
          FROM pg_catalog.pg_depend AS target_dependency
          INNER JOIN pg_catalog.pg_extension AS target_extension
              ON target_extension.oid = target_dependency.refobjid
          WHERE target_dependency.classid = 'pg_class'::pg_catalog.regclass
            AND target_dependency.objid = target_table.oid
            AND target_dependency.deptype = 'e'
      )
      AND (
          source_namespace.nspname NOT IN ('public', 'audit')
          OR (
              source_namespace.nspname = 'public'
              AND (
                  lower(source_table.relname) LIKE 'aspnet%'
                  OR source_table.relname = '__EFMigrationsHistory'
              )
          )
          OR EXISTS (
              SELECT 1
              FROM pg_catalog.pg_depend AS source_dependency
              INNER JOIN pg_catalog.pg_extension AS source_extension
                  ON source_extension.oid = source_dependency.refobjid
              WHERE source_dependency.classid = 'pg_class'::pg_catalog.regclass
                AND source_dependency.objid = source_table.oid
                AND source_dependency.deptype = 'e'
          )
      )
      AND NOT (
          source_namespace.nspname = 'public'
          AND source_table.relname = 'AspNetUsers'
          AND target_namespace.nspname = 'public'
          AND target_table.relname = 'employees'
      )
    LIMIT 1;

    IF unexpected_fk IS NOT NULL THEN
        RAISE EXCEPTION
            'Refusing reset: a retained table has an FK to reset data: %.',
            unexpected_fk;
    END IF;
END;
$guard$;

SELECT
    'Before reset' AS checkpoint,
    (SELECT count(*) FROM public."AspNetUsers") AS users,
    (SELECT count(*) FROM public."AspNetRoles") AS roles,
    (SELECT count(*) FROM public."AspNetUserRoles") AS user_roles,
    (SELECT count(*) FROM public."AspNetUserClaims") AS user_claims,
    (SELECT count(*) FROM public."AspNetRoleClaims") AS role_claims,
    (SELECT count(*) FROM public."AspNetUserLogins") AS user_logins,
    (SELECT count(*) FROM public."AspNetUserTokens") AS user_tokens,
    (SELECT count(*) FROM public."__EFMigrationsHistory") AS migrations;

-- EmployeeId is an FK to public.employees. Preserve each login record but
-- detach it from the employee row that is about to be removed.
UPDATE public."AspNetUsers"
SET "EmployeeId" = NULL
WHERE "EmployeeId" IS NOT NULL;

-- PostgreSQL does not allow TRUNCATE employees while the retained user table
-- owns this FK, even after every EmployeeId has been set to NULL. Capture and
-- detach it transactionally so it can be restored exactly after the reset.
CREATE TEMP TABLE reset_user_employee_fks (
    constraint_name text NOT NULL,
    constraint_definition text NOT NULL
) ON COMMIT DROP;

INSERT INTO reset_user_employee_fks (constraint_name, constraint_definition)
SELECT
    constraint_row.conname,
    pg_catalog.pg_get_constraintdef(constraint_row.oid, false)
FROM pg_catalog.pg_constraint AS constraint_row
WHERE constraint_row.contype = 'f'
  AND constraint_row.conrelid = 'public."AspNetUsers"'::pg_catalog.regclass
  AND constraint_row.confrelid = 'public.employees'::pg_catalog.regclass;

DO $detach$
DECLARE
    constraint_record record;
BEGIN
    FOR constraint_record IN
        SELECT constraint_name
        FROM reset_user_employee_fks
    LOOP
        EXECUTE format(
            'ALTER TABLE public."AspNetUsers" DROP CONSTRAINT %I',
            constraint_record.constraint_name);
    END LOOP;
END;
$detach$;

DO $reset$
DECLARE
    tables_to_truncate text;
BEGIN
    SELECT string_agg(
               format('%I.%I', namespace_row.nspname, table_row.relname),
               ', ' ORDER BY namespace_row.nspname, table_row.relname)
    INTO tables_to_truncate
    FROM pg_catalog.pg_class AS table_row
    INNER JOIN pg_catalog.pg_namespace AS namespace_row
        ON namespace_row.oid = table_row.relnamespace
    WHERE table_row.relkind IN ('r', 'p')
      AND namespace_row.nspname IN ('public', 'audit')
      AND NOT table_row.relispartition
      AND NOT (
          namespace_row.nspname = 'public'
          AND (
              lower(table_row.relname) LIKE 'aspnet%'
              OR table_row.relname = '__EFMigrationsHistory'
          )
      )
      -- Never touch a table owned by a PostgreSQL extension.
      AND NOT EXISTS (
          SELECT 1
          FROM pg_catalog.pg_depend AS dependency_row
          INNER JOIN pg_catalog.pg_extension AS extension_row
              ON extension_row.oid = dependency_row.refobjid
          WHERE dependency_row.classid = 'pg_class'::pg_catalog.regclass
            AND dependency_row.objid = table_row.oid
            AND dependency_row.deptype = 'e'
      );

    IF tables_to_truncate IS NOT NULL THEN
        -- Intentionally omit CASCADE. Any unexpected FK from a retained table
        -- makes the operation fail and roll back instead of deleting Identity.
        EXECUTE format('TRUNCATE TABLE %s RESTART IDENTITY', tables_to_truncate);
    END IF;
END;
$reset$;

DO $restore$
DECLARE
    constraint_record record;
BEGIN
    FOR constraint_record IN
        SELECT constraint_name, constraint_definition
        FROM reset_user_employee_fks
    LOOP
        EXECUTE format(
            'ALTER TABLE public."AspNetUsers" ADD CONSTRAINT %I %s',
            constraint_record.constraint_name,
            constraint_record.constraint_definition);
    END LOOP;
END;
$restore$;

DO $verify$
DECLARE
    table_record record;
    remaining_rows bigint;
BEGIN
    FOR table_record IN
        SELECT namespace_row.nspname, table_row.relname
        FROM pg_catalog.pg_class AS table_row
        INNER JOIN pg_catalog.pg_namespace AS namespace_row
            ON namespace_row.oid = table_row.relnamespace
        WHERE table_row.relkind IN ('r', 'p')
          AND namespace_row.nspname IN ('public', 'audit')
          AND NOT table_row.relispartition
          AND NOT (
              namespace_row.nspname = 'public'
              AND (
                  lower(table_row.relname) LIKE 'aspnet%'
                  OR table_row.relname = '__EFMigrationsHistory'
              )
          )
          AND NOT EXISTS (
              SELECT 1
              FROM pg_catalog.pg_depend AS dependency_row
              INNER JOIN pg_catalog.pg_extension AS extension_row
                  ON extension_row.oid = dependency_row.refobjid
              WHERE dependency_row.classid = 'pg_class'::pg_catalog.regclass
                AND dependency_row.objid = table_row.oid
                AND dependency_row.deptype = 'e'
          )
    LOOP
        EXECUTE format(
            'SELECT count(*) FROM %I.%I',
            table_record.nspname,
            table_record.relname)
        INTO remaining_rows;

        IF remaining_rows <> 0 THEN
            RAISE EXCEPTION
                'Verification failed: %.% still has % row(s).',
                table_record.nspname,
                table_record.relname,
                remaining_rows;
        END IF;
    END LOOP;
END;
$verify$;

SELECT
    'After reset' AS checkpoint,
    (SELECT count(*) FROM public."AspNetUsers") AS users,
    (SELECT count(*) FROM public."AspNetRoles") AS roles,
    (SELECT count(*) FROM public."AspNetUserRoles") AS user_roles,
    (SELECT count(*) FROM public."AspNetUserClaims") AS user_claims,
    (SELECT count(*) FROM public."AspNetRoleClaims") AS role_claims,
    (SELECT count(*) FROM public."AspNetUserLogins") AS user_logins,
    (SELECT count(*) FROM public."AspNetUserTokens") AS user_tokens,
    (SELECT count(*) FROM public."__EFMigrationsHistory") AS migrations;

COMMIT;
SQL

echo "Đã reset dữ liệu nghiệp vụ. Dữ liệu Identity và lịch sử migration được giữ nguyên."
echo "Lưu ý: mọi liên kết AspNetUsers.EmployeeId đã được đặt về NULL."
