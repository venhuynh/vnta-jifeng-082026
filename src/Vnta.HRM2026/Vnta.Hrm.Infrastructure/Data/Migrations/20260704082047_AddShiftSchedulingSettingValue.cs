using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftSchedulingSettingValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS public.shift_scheduling_settings (
                    "Id" uuid NOT NULL,
                    "ClassificationType" integer NOT NULL,
                    "Value" character varying(500) NULL,
                    "AssignmentScopeMode" integer NOT NULL,
                    "IsActive" boolean NOT NULL DEFAULT TRUE,
                    "CreatedAtUtc" timestamp without time zone NOT NULL,
                    "UpdatedAtUtc" timestamp without time zone NULL,
                    CONSTRAINT "PK_shift_scheduling_settings" PRIMARY KEY ("Id")
                );

                ALTER TABLE public.shift_scheduling_settings
                ADD COLUMN IF NOT EXISTS "Value" character varying(500) NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.shift_scheduling_settings
                DROP COLUMN IF EXISTS "Value";
                """);
        }
    }
}
