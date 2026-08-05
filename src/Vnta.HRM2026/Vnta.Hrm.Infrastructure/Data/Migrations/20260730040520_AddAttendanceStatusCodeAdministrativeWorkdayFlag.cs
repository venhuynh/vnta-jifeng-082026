using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceStatusCodeAdministrativeWorkdayFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('public.attendance_status_codes') IS NOT NULL
                        AND NOT EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'attendance_status_codes'
                              AND column_name = 'Cong_Hanh_Chinh') THEN
                        ALTER TABLE public.attendance_status_codes
                            ADD COLUMN "Cong_Hanh_Chinh" boolean NOT NULL DEFAULT FALSE;

                        -- Giữ nguyên tập CODE đang đóng góp Công HC trong luồng chuyên cần.
                        IF EXISTS (
                            SELECT 1
                            FROM information_schema.columns
                            WHERE table_schema = 'public'
                              AND table_name = 'attendance_status_codes'
                              AND column_name = 'Phu_Cap_Chuyen_Can') THEN
                            UPDATE public.attendance_status_codes
                            SET "Cong_Hanh_Chinh" = "Phu_Cap_Chuyen_Can";
                        END IF;
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
                            DROP COLUMN IF EXISTS "Cong_Hanh_Chinh";
                    END IF;
                END $$;
                """);
        }
    }
}
