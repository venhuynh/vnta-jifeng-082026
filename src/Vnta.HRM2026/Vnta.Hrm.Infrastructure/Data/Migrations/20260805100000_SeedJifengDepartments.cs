using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <summary>
/// Inserts the approved Jifeng department structure without relying on application-startup seeding.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260805100000_SeedJifengDepartments")]
public partial class SeedJifengDepartments : Migration
{
    private const string SeedTimestampSql = "TIMESTAMP '2026-08-05 10:00:00'";

    /// <summary>
    /// Canonical rows from the approved Jifeng tree. The display root "Jifeng" is intentionally not persisted.
    /// </summary>
    public static IReadOnlyList<JifengDepartmentSeedRow> SeedRows { get; } =
    [
        new(new Guid("447ebdfe-3d44-4f20-b1ca-dd7357da46a1"), "PB005", "Hành Chính Nhân Sự/ Tuân thủ", "Hành Chính Nhân Sự/ Tuân thủ"),
        new(new Guid("ee87546e-bb89-4546-a7fb-ed57bf9409a1"), "02", "Hành Chính Nhân Sự/ Tuân thủ", "An toàn và tuân thủ"),
        new(new Guid("e31710bc-491b-4f1b-b919-f76bbee06ae5"), "04", "Hành Chính Nhân Sự/ Tuân thủ", "Bảo vệ"),
        new(new Guid("a2c69487-d557-43a0-8463-8d36c95aae8e"), "01", "Hành Chính Nhân Sự/ Tuân thủ", "Hành chính nhân sự"),
        new(new Guid("048d203a-99bb-444e-8741-dee01568969f"), "05", "Hành Chính Nhân Sự/ Tuân thủ", "Nhà ăn"),
        new(new Guid("82052536-0510-46f7-8e93-7487289ba3c9"), "03", "Hành Chính Nhân Sự/ Tuân thủ", "Tạp vụ"),
        new(new Guid("657209be-f0a8-4dca-9c6a-c4408919619c"), "YT", "Hành Chính Nhân Sự/ Tuân thủ", "Y tế"),
        new(new Guid("fc2ba40d-00b3-41e8-a32a-0d531616de18"), "KT", "Kế toán", "Kế toán"),
        new(new Guid("9cd0cbeb-dc02-4bd0-92e3-9a3b4d3511f4"), "PB001", "Mặc định", "Mặc định"),
        new(new Guid("d2b4a9ce-7f62-40a0-b366-888c6b062c44"), "PB002", "Phát Triển Sản Phẩm/ Quản Lý Chất Lượng", "Phát Triển Sản Phẩm/ Quản Lý Chất Lượng"),
        new(new Guid("61b0a9b2-fb2d-4e9b-8ee7-fbbc1c11d3f6"), "CL", "Phát Triển Sản Phẩm/ Quản Lý Chất Lượng", "Chất lượng"),
        new(new Guid("cbb812cc-e1e4-46fa-b19a-046bb8755401"), "KP", "Phát Triển Sản Phẩm/ Quản Lý Chất Lượng", "Khai phá"),
        new(new Guid("bb018e2d-19d8-4b93-b179-00dac2f4e32f"), "PB017", "Phòng sản xuất/生产", "Phòng sản xuất/生产"),
        new(new Guid("bb5c80ad-a0ce-4e90-a8a9-1ac482fa6cf4"), "SX", "Sản Xuất", "Sản Xuất"),
        new(new Guid("0c83787f-fde9-448d-bed5-433f6d5673ee"), "BTKT", "Sản Xuất", "Bảo trì/ Kỹ thuật"),
        new(new Guid("4183babd-5248-4f61-b58a-ffdee2ddd3e1"), "JF/PACK", "Sản Xuất", "Đóng gói"),
        new(new Guid("3da636f3-338a-47dc-90b4-ed6fce695be3"), "GC", "Sản Xuất", "Gia công"),
        new(new Guid("3691d259-e29a-4056-b11e-78e023ae0c14"), "KH", "Sản Xuất", "Kế hoạch"),
        new(new Guid("8eabe5c7-864b-4c5b-9c23-9a51211c320b"), "TM", "Sản Xuất", "Thu mua"),
        new(new Guid("345fedcb-8846-4a98-8dcc-cb05489286bd"), "TK", "Sản Xuất", "Văn phòng xưởng"),
        new(new Guid("f377b4ad-d044-4b27-8946-fb12c21b74c4"), "JF/BOD", "Ban giám đốc điều hành", "Ban giám đốc điều hành"),
        new(new Guid("87d50a40-7146-4e3d-815b-8ff24b30bb04"), "JF/LOG", "Xuất Nhập Khẩu / Sales", "Xuất Nhập Khẩu / Sales"),
        new(new Guid("29fd0c6a-fc87-4f3a-adc8-6699113b2fe6"), "JF/WH", "Xuất Nhập Khẩu / Sales", "Kho"),
        new(new Guid("8178f2ad-d140-4e00-a9c0-659d543fde0b"), "LXN", "Xuất Nhập Khẩu / Sales", "Lái xe nâng"),
        new(new Guid("33c9c658-f13d-4cb0-889e-ba24d85e5b0c"), "SL", "Xuất Nhập Khẩu / Sales", "Sales"),
        new(new Guid("c504a1e2-3af0-42ad-83bf-d1851fba9afe"), "JF/IM", "Xuất Nhập Khẩu / Sales", "Xuất nhập khẩu")
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var seedValues = string.Join(
            ",\n            ",
            SeedRows.Select(BuildSeedValueSql));

        migrationBuilder.Sql($$"""
            LOCK TABLE public.departments IN SHARE MODE;

            CREATE TEMPORARY TABLE jifeng_department_seed
            (
                "Id" uuid NOT NULL,
                "Code" character varying(50) NOT NULL,
                "CenterName" character varying(200) NOT NULL,
                "DepartmentOrWorkshopName" character varying(200) NOT NULL,
                "TeamName" character varying(200) NULL,
                "GroupName" character varying(200) NULL,
                "Notes" character varying(1000) NULL,
                "Status" integer NOT NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL
            ) ON COMMIT DROP;

            INSERT INTO jifeng_department_seed
            (
                "Id",
                "Code",
                "CenterName",
                "DepartmentOrWorkshopName",
                "TeamName",
                "GroupName",
                "Notes",
                "Status",
                "CreatedAtUtc",
                "UpdatedAtUtc"
            )
            VALUES
                {{seedValues}};

            DO $$
            BEGIN
                IF EXISTS
                (
                    SELECT 1
                    FROM jifeng_department_seed
                    GROUP BY lower(btrim("Code"))
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Cannot seed Jifeng departments because the seed payload contains duplicate codes.';
                END IF;

                IF EXISTS
                (
                    SELECT 1
                    FROM public.departments AS existing
                    INNER JOIN jifeng_department_seed AS seed
                        ON lower(btrim(existing."Code")) = lower(btrim(seed."Code"))
                    WHERE existing."Id" <> seed."Id"
                ) THEN
                    RAISE EXCEPTION 'Cannot seed Jifeng departments because an existing department uses a conflicting code.';
                END IF;

                IF EXISTS
                (
                    SELECT 1
                    FROM public.departments AS existing
                    INNER JOIN jifeng_department_seed AS seed
                        ON existing."Id" = seed."Id"
                    WHERE lower(btrim(existing."Code")) <> lower(btrim(seed."Code"))
                ) THEN
                    RAISE EXCEPTION 'Cannot seed Jifeng departments because a seed identifier is already assigned to another code.';
                END IF;
            END
            $$;

            INSERT INTO public.departments
            (
                "Id",
                "Code",
                "CenterName",
                "DepartmentOrWorkshopName",
                "TeamName",
                "GroupName",
                "Notes",
                "Status",
                "CreatedAtUtc",
                "UpdatedAtUtc"
            )
            SELECT
                "Id",
                "Code",
                "CenterName",
                "DepartmentOrWorkshopName",
                "TeamName",
                "GroupName",
                "Notes",
                "Status",
                "CreatedAtUtc",
                "UpdatedAtUtc"
            FROM jifeng_department_seed
            ON CONFLICT ("Id") DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Department rows can be referenced by employees, so rolling back must not delete operational data.
    }

    private static string BuildSeedValueSql(JifengDepartmentSeedRow row) =>
        $"('{row.Id:D}'::uuid, {ToSqlLiteral(row.Code)}, {ToSqlLiteral(row.CenterName)}, " +
        $"{ToSqlLiteral(row.DepartmentOrWorkshopName)}, {ToSqlLiteral(row.TeamName)}, " +
        $"{ToSqlLiteral(row.GroupName)}, {ToSqlLiteral(row.Notes)}, {row.Status}, " +
        $"{SeedTimestampSql}, NULL)";

    private static string ToSqlLiteral(string? value) =>
        value is null ? "NULL" : $"'{value.Replace("'", "''")}'";
}

public sealed record JifengDepartmentSeedRow(
    Guid Id,
    string Code,
    string CenterName,
    string DepartmentOrWorkshopName,
    string? TeamName = null,
    string? GroupName = null,
    string? Notes = null,
    int Status = 0);
