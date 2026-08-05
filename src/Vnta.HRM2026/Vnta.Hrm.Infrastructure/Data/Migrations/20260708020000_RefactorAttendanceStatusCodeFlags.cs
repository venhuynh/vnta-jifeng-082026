using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260708020000_RefactorAttendanceStatusCodeFlags")]
    /// <inheritdoc />
    public partial class RefactorAttendanceStatusCodeFlags : Migration
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
                        ADD COLUMN IF NOT EXISTS "Cong_Tang_Ca" boolean NOT NULL DEFAULT FALSE,
                        ADD COLUMN IF NOT EXISTS "Phu_Cap_Trach_Nhiem_Tinh_Nang_Suat" boolean NOT NULL DEFAULT FALSE,
                        ADD COLUMN IF NOT EXISTS "Phu_Cap_Doc_Hai" boolean NOT NULL DEFAULT FALSE,
                        ADD COLUMN IF NOT EXISTS "Phu_Cap_Trach_Nhiem_Khac" boolean NOT NULL DEFAULT FALSE,
                        ADD COLUMN IF NOT EXISTS "Phu_Cap_Phep_Le" boolean NOT NULL DEFAULT FALSE,
                        ADD COLUMN IF NOT EXISTS "Phu_Cap_Trach_Nhiem_Khong_Tinh_Nang_Suat" boolean NOT NULL DEFAULT FALSE;

                        ALTER TABLE public.attendance_status_codes
                        DROP COLUMN IF EXISTS "PaymentModelId",
                        DROP COLUMN IF EXISTS "WorkdayCredit",
                        DROP COLUMN IF EXISTS "PaidWorkdayCredit",
                        DROP COLUMN IF EXISTS "SocialInsuranceWorkday",
                        DROP COLUMN IF EXISTS "DeductsAnnualLeave",
                        DROP COLUMN IF EXISTS "RequiresAttachment",
                        DROP COLUMN IF EXISTS "RequiresApproval",
                        DROP COLUMN IF EXISTS "DisplayOrder";
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
                        ADD COLUMN IF NOT EXISTS "PaymentModelId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000',
                        ADD COLUMN IF NOT EXISTS "WorkdayCredit" numeric(4,2) NOT NULL DEFAULT 0,
                        ADD COLUMN IF NOT EXISTS "PaidWorkdayCredit" numeric(4,2) NOT NULL DEFAULT 0,
                        ADD COLUMN IF NOT EXISTS "SocialInsuranceWorkday" numeric(4,2) NOT NULL DEFAULT 0,
                        ADD COLUMN IF NOT EXISTS "DeductsAnnualLeave" boolean NOT NULL DEFAULT FALSE,
                        ADD COLUMN IF NOT EXISTS "RequiresAttachment" boolean NOT NULL DEFAULT FALSE,
                        ADD COLUMN IF NOT EXISTS "RequiresApproval" boolean NOT NULL DEFAULT TRUE,
                        ADD COLUMN IF NOT EXISTS "DisplayOrder" integer NOT NULL DEFAULT 0;

                        ALTER TABLE public.attendance_status_codes
                        DROP COLUMN IF EXISTS "Cong_Tang_Ca",
                        DROP COLUMN IF EXISTS "Phu_Cap_Trach_Nhiem_Tinh_Nang_Suat",
                        DROP COLUMN IF EXISTS "Phu_Cap_Doc_Hai",
                        DROP COLUMN IF EXISTS "Phu_Cap_Trach_Nhiem_Khac",
                        DROP COLUMN IF EXISTS "Phu_Cap_Phep_Le",
                        DROP COLUMN IF EXISTS "Phu_Cap_Trach_Nhiem_Khong_Tinh_Nang_Suat";
                    END IF;
                END $$;
                """);
        }
    }
}
