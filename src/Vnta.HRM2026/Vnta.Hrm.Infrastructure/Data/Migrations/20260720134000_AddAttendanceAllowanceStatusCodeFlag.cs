using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <summary>
/// Chuyển cấu hình mã được tính công chuyên cần sang danh mục kết quả chấm công.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260720134000_AddAttendanceAllowanceStatusCodeFlag")]
public partial class AddAttendanceAllowanceStatusCodeFlag : Migration
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
                          AND column_name = 'Phu_Cap_Chuyen_Can') THEN
                    ALTER TABLE public.attendance_status_codes
                    ADD COLUMN "Phu_Cap_Chuyen_Can" boolean NOT NULL DEFAULT FALSE;

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
                    DROP COLUMN IF EXISTS "Phu_Cap_Chuyen_Can";
                END IF;
            END $$;
            """);
    }
}
