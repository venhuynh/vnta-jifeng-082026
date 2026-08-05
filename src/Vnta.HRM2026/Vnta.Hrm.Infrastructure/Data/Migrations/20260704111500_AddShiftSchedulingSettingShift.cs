using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vnta.Hrm.Infrastructure.Data;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260704111500_AddShiftSchedulingSettingShift")]
    public partial class AddShiftSchedulingSettingShift : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.shift_scheduling_settings
                ADD COLUMN IF NOT EXISTS "ShiftId" uuid NULL;

                CREATE INDEX IF NOT EXISTS "IX_shift_scheduling_settings_ShiftId"
                ON public.shift_scheduling_settings ("ShiftId");

                DO $$
                BEGIN
                    IF to_regclass('public.shifts') IS NOT NULL
                        AND NOT EXISTS (
                            SELECT 1
                            FROM pg_constraint
                            WHERE conname = 'FK_shift_scheduling_settings_shifts_ShiftId'
                        )
                    THEN
                        ALTER TABLE public.shift_scheduling_settings
                        ADD CONSTRAINT "FK_shift_scheduling_settings_shifts_ShiftId"
                        FOREIGN KEY ("ShiftId") REFERENCES public.shifts ("Id")
                        ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_shift_scheduling_settings_shifts_ShiftId'
                    )
                    THEN
                        ALTER TABLE public.shift_scheduling_settings
                        DROP CONSTRAINT "FK_shift_scheduling_settings_shifts_ShiftId";
                    END IF;
                END $$;

                DROP INDEX IF EXISTS public."IX_shift_scheduling_settings_ShiftId";

                ALTER TABLE public.shift_scheduling_settings
                DROP COLUMN IF EXISTS "ShiftId";
                """);
        }
    }
}
