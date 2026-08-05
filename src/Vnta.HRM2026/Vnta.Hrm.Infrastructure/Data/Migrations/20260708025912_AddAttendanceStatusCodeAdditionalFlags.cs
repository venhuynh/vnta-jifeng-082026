using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceStatusCodeAdditionalFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('public.attendance_status_codes') IS NOT NULL THEN
                        ALTER TABLE public.attendance_status_codes
                        ADD COLUMN IF NOT EXISTS "Khau_Tru_Tam_Ung" boolean NOT NULL DEFAULT FALSE,
                        ADD COLUMN IF NOT EXISTS "Phu_Cap_Tham_Nien" boolean NOT NULL DEFAULT FALSE;
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
                    IF to_regclass('public.attendance_status_codes') IS NOT NULL THEN
                        ALTER TABLE public.attendance_status_codes
                        DROP COLUMN IF EXISTS "Khau_Tru_Tam_Ung",
                        DROP COLUMN IF EXISTS "Phu_Cap_Tham_Nien";
                    END IF;
                END $$;
                """);
        }
    }
}
