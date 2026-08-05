using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RepairResponsibilityAllowanceGradePositionsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TABLE IF NOT EXISTS "payroll_allowance_responsibility_grade_positions" (
                    "Id" uuid NOT NULL,
                    "Year" integer NOT NULL,
                    "Month" integer NOT NULL,
                    "GradeId" uuid NOT NULL,
                    "PositionId" uuid NOT NULL,
                    "IsActive" boolean NOT NULL DEFAULT TRUE,
                    "Note" character varying(500),
                    "CreatedAtUtc" timestamp without time zone NOT NULL,
                    "UpdatedAtUtc" timestamp without time zone,
                    CONSTRAINT "PK_payroll_allowance_responsibility_grade_positions" PRIMARY KEY ("Id"),
                    CONSTRAINT "CK_payroll_allowance_responsibility_grade_positions_Month"
                        CHECK ("Month" BETWEEN 1 AND 12)
                );

                CREATE UNIQUE INDEX IF NOT EXISTS
                    "UX_payroll_allowance_responsibility_grade_positions_Year_Month_PositionId"
                    ON "payroll_allowance_responsibility_grade_positions" ("Year", "Month", "PositionId");

                CREATE INDEX IF NOT EXISTS
                    "IX_payroll_allowance_responsibility_grade_positions_Year_Month_GradeId"
                    ON "payroll_allowance_responsibility_grade_positions" ("Year", "Month", "GradeId");

                DO $$
                BEGIN
                    IF to_regclass('payroll_allowance_responsibility_grade') IS NOT NULL
                       AND NOT EXISTS (
                           SELECT 1 FROM pg_constraint
                           WHERE conname = 'FK_payroll_allowance_responsibility_grade_positions_payroll_allowance_responsibility_grade_GradeId'
                       ) THEN
                        ALTER TABLE "payroll_allowance_responsibility_grade_positions"
                            ADD CONSTRAINT "FK_payroll_allowance_responsibility_grade_positions_payroll_allowance_responsibility_grade_GradeId"
                            FOREIGN KEY ("GradeId")
                            REFERENCES "payroll_allowance_responsibility_grade" ("Id")
                            ON DELETE RESTRICT;
                    END IF;

                    IF to_regclass('positions') IS NOT NULL
                       AND NOT EXISTS (
                           SELECT 1 FROM pg_constraint
                           WHERE conname = 'FK_payroll_allowance_responsibility_grade_positions_positions_PositionId'
                       ) THEN
                        ALTER TABLE "payroll_allowance_responsibility_grade_positions"
                            ADD CONSTRAINT "FK_payroll_allowance_responsibility_grade_positions_positions_PositionId"
                            FOREIGN KEY ("PositionId")
                            REFERENCES "positions" ("Id")
                            ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This is a repair migration. Keep the repaired table on rollback to
            // avoid deleting existing position-grade mappings and their data.
        }
    }
}
