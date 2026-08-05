using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260704103000_EnsureEmployeeBiometricOneToManyRelations")]
public sealed class EnsureEmployeeBiometricOneToManyRelations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF to_regclass('public.device_user_profiles') IS NOT NULL
                   AND to_regclass('public.employees') IS NOT NULL THEN
                    DELETE FROM "device_user_profiles" child
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM "employees" parent
                        WHERE parent."Id" = child."EmployeeId");

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_device_user_profiles_employees_EmployeeId') THEN
                        ALTER TABLE "device_user_profiles"
                        ADD CONSTRAINT "FK_device_user_profiles_employees_EmployeeId"
                        FOREIGN KEY ("EmployeeId") REFERENCES "employees" ("Id") ON DELETE RESTRICT;
                    END IF;
                END IF;
            END
            $$;

            DO $$
            BEGIN
                IF to_regclass('public.fingerprint_templates') IS NOT NULL
                   AND to_regclass('public.employees') IS NOT NULL THEN
                    DELETE FROM "fingerprint_templates" child
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM "employees" parent
                        WHERE parent."Id" = child."EmployeeId");

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_fingerprint_templates_employees_EmployeeId') THEN
                        ALTER TABLE "fingerprint_templates"
                        ADD CONSTRAINT "FK_fingerprint_templates_employees_EmployeeId"
                        FOREIGN KEY ("EmployeeId") REFERENCES "employees" ("Id") ON DELETE RESTRICT;
                    END IF;
                END IF;
            END
            $$;

            DO $$
            BEGIN
                IF to_regclass('public.bio_photos') IS NOT NULL
                   AND to_regclass('public.employees') IS NOT NULL THEN
                    DELETE FROM "bio_photos" child
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM "employees" parent
                        WHERE parent."Id" = child."EmployeeId");

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_bio_photos_employees_EmployeeId') THEN
                        ALTER TABLE "bio_photos"
                        ADD CONSTRAINT "FK_bio_photos_employees_EmployeeId"
                        FOREIGN KEY ("EmployeeId") REFERENCES "employees" ("Id") ON DELETE RESTRICT;
                    END IF;
                END IF;
            END
            $$;

            DO $$
            BEGIN
                IF to_regclass('public.user_pictures') IS NOT NULL
                   AND to_regclass('public.employees') IS NOT NULL THEN
                    DELETE FROM "user_pictures" child
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM "employees" parent
                        WHERE parent."Id" = child."EmployeeId");

                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_user_pictures_employees_EmployeeId') THEN
                        ALTER TABLE "user_pictures"
                        ADD CONSTRAINT "FK_user_pictures_employees_EmployeeId"
                        FOREIGN KEY ("EmployeeId") REFERENCES "employees" ("Id") ON DELETE RESTRICT;
                    END IF;
                END IF;
            END
            $$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF to_regclass('public.device_user_profiles') IS NOT NULL THEN
                    ALTER TABLE "device_user_profiles"
                    DROP CONSTRAINT IF EXISTS "FK_device_user_profiles_employees_EmployeeId";
                END IF;
            END
            $$;

            DO $$
            BEGIN
                IF to_regclass('public.fingerprint_templates') IS NOT NULL THEN
                    ALTER TABLE "fingerprint_templates"
                    DROP CONSTRAINT IF EXISTS "FK_fingerprint_templates_employees_EmployeeId";
                END IF;
            END
            $$;

            DO $$
            BEGIN
                IF to_regclass('public.bio_photos') IS NOT NULL THEN
                    ALTER TABLE "bio_photos"
                    DROP CONSTRAINT IF EXISTS "FK_bio_photos_employees_EmployeeId";
                END IF;
            END
            $$;

            DO $$
            BEGIN
                IF to_regclass('public.user_pictures') IS NOT NULL THEN
                    ALTER TABLE "user_pictures"
                    DROP CONSTRAINT IF EXISTS "FK_user_pictures_employees_EmployeeId";
                END IF;
            END
            $$;
            """);
    }
}
