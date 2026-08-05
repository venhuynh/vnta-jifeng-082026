using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable enable

namespace Vnta.Hrm.Infrastructure.Data.Migrations;

/// <summary>
/// Inserts the canonical Jifeng position catalogue without relying on application-startup seeding.
/// </summary>
[DbContext(typeof(ApplicationDbContext))]
[Migration("20260805110000_SeedJifengPositions")]
public partial class SeedJifengPositions : Migration
{
    private const string SeedTimestampSql = "TIMESTAMP '2026-08-05 11:00:00'";

    /// <summary>
    /// Canonical rows from the <c>Chuc danh</c> worksheet. The first occurrence wins for a duplicated source code.
    /// </summary>
    public static IReadOnlyList<JifengPositionSeedRow> SeedRows { get; } =
    [
        new(new Guid("83bbfb1e-c377-43ba-aa30-834e9b7f715e"), "A", "Quản trị viên"),
        new(new Guid("71e98524-f21d-4788-9702-6f50e6c3d5ee"), "CD001", "Phó Tổng Giám Đốc (CN)/Deputy General Manager (CN)"),
        new(new Guid("bd68d374-1e25-4ee8-aa73-5efe2b5f55dd"), "CD002", "Phó Tổng Giám Đốc (VN)/Deputy General Manager (VN)"),
        new(new Guid("25600d1e-86e2-4427-8e33-ffc514adc772"), "CD003", "Giám Đốc"),
        new(new Guid("b5b30b5e-d3f2-4305-805b-cbf6162ea7b5"), "CD005", "Giám đốc hành chính nhân sự/ Tuân thủ"),
        new(new Guid("7fbe8ee8-68fd-42c6-abc2-52094b79c844"), "CD006", "Giám Đốc Quản lý chất lượng & phát triển sản phẩm"),
        new(new Guid("44389d2d-2f3d-46df-b050-fd446f680e1e"), "CD007", "Nhân viên bảo trì"),
        new(new Guid("03fea07b-5e34-4e0a-89c4-991e8e0f56f4"), "CD009", "Nhân Viên phiên dịch"),
        new(new Guid("7434430b-a029-4f08-a5cf-f42ecc76bea9"), "CD010", "Nhân Viên kế toán"),
        new(new Guid("1d5b3ca2-68f0-4b11-b426-fb07d127881d"), "CD013", "Giám đốc Xuất nhập khẩu & Sale"),
        new(new Guid("0ce74dac-d06c-4175-8136-79922a5fadcb"), "CD014", "Tạp vụ"),
        new(new Guid("3c22e15a-de31-4aa1-8159-a084873f38a3"), "CD015", "Nhân Viên hành chính nhân sự/ Tuân thủ"),
        new(new Guid("a63ef93e-3c38-49ec-bc18-a52ee70ee12b"), "CD016", "Nhân Viên Xuất nhập khẩu"),
        new(new Guid("3e667d49-e038-400b-acbc-90785e85fc66"), "CD017", "Chủ quản sản xuất"),
        new(new Guid("a24e71f7-ceb2-4b32-acf5-ef9f3b137113"), "CD020", "Nhân viên kỹ thuật"),
        new(new Guid("596df181-dd10-4f75-a6f7-b7675785f999"), "CD021", "Nhân viên kho"),
        new(new Guid("8985fb04-d1ea-42ee-8e34-324f16868754"), "CD022", "Kế toán trưởng"),
        new(new Guid("883f8cae-37b7-49a6-a533-c67ef189a42c"), "CD039", "Xe nâng"),
        new(new Guid("8384d10c-ce9b-47c6-bcf4-ce4d3c05f21e"), "CD040", "Mài bóng"),
        new(new Guid("f37da1dd-5123-4bed-a42b-4fd4329e21dc"), "CD041", "Nhân viên QC"),
        new(new Guid("7cd393c4-c6e4-4238-a227-244b30e0fdbd"), "CD042", "Nhân viên khai phá/R&D"),
        new(new Guid("f9308ea2-8e2a-4077-9630-805356ae3e0c"), "CD043", "Nhân viên QA"),
        new(new Guid("0300b789-7c7f-416c-a837-aa8e2c5a39e3"), "CD044", "Bảo vệ"),
        new(new Guid("38600d96-4c3a-43ef-9b7f-8801b1bfbbc2"), "CD045", "Nhân viên nhà ăn"),
        new(new Guid("cc509681-fec3-4b40-93c5-0302bffa453f"), "CD046", "Trợ lý TGĐ"),
        new(new Guid("d8faee28-cc7b-4b6c-b6bc-db2829f17975"), "CD047", "Nhân viên thu mua"),
        new(new Guid("e1213231-3a44-49f3-95c3-4bf00dc7d9b3"), "CD048", "Nhân viên kế hoạch sản xuất"),
        new(new Guid("2561f4aa-4c2a-408c-9432-7cc2f9c069cd"), "CD049", "Nhân viên HSE"),
        new(new Guid("c5e66ce5-5eaf-4e3f-a13f-b4a90da692cc"), "CD050", "Nhân viên thống kê"),
        new(new Guid("9dcdcbe2-9560-42a3-9042-b5a1d277a658"), "CD051", "Trợ lý chuyền đóng gói"),
        new(new Guid("f2210c34-70a9-4316-a12a-67b62ca807bf"), "CD052", "Quản đốc phân xưởng"),
        new(new Guid("8cd53971-a658-4000-b061-ea73d6461c95"), "CD053", "Chủ quản QC"),
        new(new Guid("73186e57-2fa5-4b94-beea-c66c4557dd28"), "CD054", "Chủ quản bảo trì"),
        new(new Guid("555175f7-3a66-4381-9347-0153b28da522"), "CD058", "Chủ quản kho"),
        new(new Guid("9f355f5c-9624-4036-9356-3513fc01e95f"), "CD086", "Công nhân vận hành máy dập, hàn"),
        new(new Guid("25bee51f-901a-4f6a-bdfc-607cc997e12e"), "CD087", "Công nhân đóng đai"),
        new(new Guid("7d22d580-1fb0-44d3-af75-22e0e9b9c838"), "CD088", "Công nhân lắp ráp"),
        new(new Guid("01b2f38b-9430-4af5-aee7-358792f8d880"), "CD089", "Chủ quản thu mua"),
        new(new Guid("d79a02c8-549d-42de-91bd-dc805189d341"), "CD090", "Tổ trưởng vận hành máy dập"),
        new(new Guid("a8fbd1d8-5e32-4aae-96d8-6cf9430757dd"), "CD091", "Ca trưởng vận hành máy tạo ống"),
        new(new Guid("60221dfb-527b-418c-bbf1-3257e2e07f54"), "CD092", "Ca trưởng vận hành máy bít đầu ống"),
        new(new Guid("8438b97f-87a2-45cd-b181-e94d78b387eb"), "CD093", "Tổ trưởng đóng gói"),
        new(new Guid("a84e7a14-0ece-410d-a879-0e359c52e1ef"), "CD094", "Ca trưởng đóng gói"),
        new(new Guid("68dc1677-3185-4fc4-bfb1-f0145473e5df"), "CD095", "Tổ trưởng vận hành máy mài bóng"),
        new(new Guid("b58b4d81-37a5-4b79-839a-3a494d9a860a"), "CD096", "Công nhân vận hành máy đóng gói ốc vít"),
        new(new Guid("5c2d49ed-6e8b-456c-a024-0713ff66fb97"), "CD097", "Nhân viên thống kê kho"),
        new(new Guid("bbe5da83-b27a-4b6e-a6be-5e5d45fb04e1"), "CD098", "Nhân viên y tế"),
        new(new Guid("074d780f-d987-474c-a948-a576630c030c"), "CD099", "Công nhân vận hành máy đánh bóng rung"),
        new(new Guid("00d930a2-e545-4173-818d-d72ed04165fd"), "CD100", "Chủ quản R&D"),
        new(new Guid("22925c4a-7b3e-496d-969c-36b33428f787"), "CD101", "Chủ nhiệm chuyền đóng gói"),
        new(new Guid("d27d4c1a-77d4-4055-8ce7-9b1bd1ff8045"), "CD102", "Công nhân dán chân pallet"),
        new(new Guid("b505be39-4938-4e26-9ee7-7c35abc3254d"), "CD103", "Tổ trưởng vận hành máy tạo ống"),
        new(new Guid("80ff21ac-d1be-44f8-8697-eff8ded0c0ef"), "CD104", "Tổ trưởng bộ phận gia công"),
        new(new Guid("743c4142-a6a5-4c3e-b954-a37194ae4a3e"), "CD105", "Công nhân điều chỉnh khuôn máy"),
        new(new Guid("6476270b-9eed-4911-8f69-cc98d6eff61f"), "CD106", "Tổ trưởng kho"),
        new(new Guid("89f572ef-2b8e-4f70-8d42-1fb4a56a613d"), "CD107", "Công nhân vận hành máy đóng gói ốc vít & dán chân pallet"),
        new(new Guid("198a70d1-aa67-4d8e-8f11-f28b0a49ed1d"), "CD108", "Phó chủ nhiệm chuyền sản xuất"),
        new(new Guid("1c43f52d-294c-4e26-adc2-49be336ee0e1"), "CD109", "Nhân viên thí nghiệm"),
        new(new Guid("e3d8e1ed-fc17-45a9-8bbc-8fa1aeec90c5"), "CV019", "Mặc định"),
        new(new Guid("7ba9aeb4-3544-46ce-9b53-703d87a2028c"), "CV025", "Nhân viên  kho"),
        new(new Guid("c14e1ebd-9854-4de5-9f54-fc6c353926da"), "CV033", "Trợ lý sản xuất"),
        new(new Guid("2ceaca04-31d3-41a9-844f-bde91ea46ba5"), "CV044", "Tổng Giám Đốc (CN)"),
        new(new Guid("8fa0006e-f7d9-4cca-b3d6-68a6f29b3107"), "CV045", "Tổng Giám Đốc (VN)"),
        new(new Guid("4b0427bc-7e8e-4d53-9e95-0153d4fa8e9f"), "CV046", "Nhân viên kỹ thuật và bảo trì khuôn"),
        new(new Guid("54affc79-997a-439d-a074-39ca3363d1da"), "CV047", "Công nhân QC"),
        new(new Guid("f30c16f3-5fed-4bb0-9d39-83ff9e8dc139"), "CV048", "Công nhân đóng gói"),
        new(new Guid("d3faf188-aaf9-4856-855d-0b12417876ae"), "CV049", "Công nhân vận hành máy hàn điểm"),
        new(new Guid("a2a37a5a-9a1b-4d06-8e00-a0f7d83b383f"), "CV050", "Công nhân vận hành máy ép PET, dập, điêu khắc laser"),
        new(new Guid("0865657f-fb2d-4d28-ac70-481a6cb3083d"), "CV051", "Công nhân"),
        new(new Guid("bdefcf93-eaf9-4925-9532-6e0b0f9ce60a"), "CV052", "Công nhân vận hành máy tạo ống"),
        new(new Guid("2c927327-9ded-45fe-b268-94ada16c36aa"), "CV053", "Công nhân vận hành máy dập, hiệu chuẩn"),
        new(new Guid("e6eb2b24-c37e-4afd-a09b-13cd728d141e"), "CV054", "Công nhân vận hành máy mài bóng"),
        new(new Guid("2304ee0d-2552-4ef9-a614-26a771bae2c4"), "CV055", "Nhân viên"),
        new(new Guid("44bd1e8a-16ef-4f33-bfce-fe748dab2695"), "CV056", "Công nhân vận hành máy bít đầu ống, xử lý nước thải"),
        new(new Guid("c8027ecf-f2a1-4edc-ba2d-5d4fcc590011"), "CV057", "Công nhân kiểm hàng"),
        new(new Guid("bdfb3aeb-4268-4040-a7e9-458f5d76e0b2"), "CV058", "Công nhân vận hành máy đánh bóng"),
        new(new Guid("95bab78e-dd5e-4fae-894f-b13f80c438b7"), "CV059", "Công nhân vận hành máy dập"),
        new(new Guid("5c4349a8-24f1-48b5-a29b-9115e9e4bca2"), "CV060", "Công nhân vận hành máy bít đầu ống"),
        new(new Guid("986aecc5-7fc2-470e-98b8-09ce96114838"), "CV061", "Công nhân vận hành máy cắt liệu"),
        new(new Guid("270f3865-7440-45f2-8a1b-4e446e8a43c2"), "CV062", "Công nhân kiểm hàng, vận hành máy đánh bóng"),
        new(new Guid("140773b8-151a-4217-bd3f-e900d61c5d7c"), "CV063", "Công nhân vận hành máy cắt lazer"),
        new(new Guid("ee0f3fe4-792f-4506-83c9-f6d8ee4395c2"), "CV064", "Công nhân vận hành máy cắt lazer, hiệu chỉnh"),
        new(new Guid("3609c180-ed11-4aff-bdd6-6bd43d8f60dc"), "CV065", "Công nhân vận hành vát mép ống, bulong"),
        new(new Guid("67861477-eb8d-4025-a6c6-07123354ac3f"), "CV066", "Nhân viên HCNS"),
        new(new Guid("27c5a1fe-6146-4179-933d-4e38d024844a"), "CV067", "Giám đốc HCNS"),
        new(new Guid("fb35cf59-4a19-4daa-8bff-e2785dee9a34"), "CV068", "Nhân viên kế hoạch"),
        new(new Guid("ec526929-a2e5-437d-831b-051f5d254e2d"), "CV069", "Thủ quỹ"),
        new(new Guid("50fcd67e-61be-4393-a841-b38b7aab9ab0"), "CV070", "Kế toán giá thành"),
        new(new Guid("0a6d366b-7d4f-42fb-8389-95554c6d299f"), "CV071", "Nhân viên khai phá"),
        new(new Guid("4b597f7b-28c6-4b53-8411-c20c5ac75fb4"), "CV072", "Lái xe nâng"),
        new(new Guid("fecc6dcf-b52a-47ee-a1d0-02fa8528231c"), "CV073", "Quản kho thành phẩm & lái xe nâng"),
        new(new Guid("0f84bee8-2033-435e-bd4d-4bfbd8e28cce"), "CV074", "Công nhân kho"),
        new(new Guid("65bb6d9c-65ae-4453-901a-a06109f9faea"), "CV075", "Công nhân vận hành xe nâng"),
        new(new Guid("53665f65-8eec-4bb4-8be2-b7df2c57cd93"), "CV076", "Nhà ăn"),
        new(new Guid("c7cb0e15-4250-4519-85d2-b7d79fe91119"), "CV077", "Giám đốc chất lượng và R&D"),
        new(new Guid("287ae25a-0692-40c1-974b-a67cf7f8fbfa"), "CV078", "Nhân viên Sales"),
        new(new Guid("8caa657c-5025-4d95-bfc0-b3deb7edcc20"), "CV079", "Phó giám đốc"),
        new(new Guid("8627a2f3-55cb-477f-a72c-7bca21d055e7"), "CV080", "Chủ nhiệm chuyền sản xuất"),
        new(new Guid("0ceb9bed-7b80-44c4-a536-4386fa0d24a3"), "CV081", "Chủ quản chuyền đóng gói"),
        new(new Guid("d995a0ee-89a2-4e84-ae65-c8bb862bfd4d"), "CV082", "Tạp vụ văn phòng"),
        new(new Guid("fe03c465-4c02-408b-a79e-79c7701f9c6d"), "CV083", "Tạp vụ xưởng"),
        new(new Guid("b6a7fe4a-d4d4-4041-a716-70c882620940"), "CV084", "Nhân viên XNK")
    ];

    /// <summary>
    /// Source rows omitted because their code was already assigned to a preceding canonical row.
    /// A follow-up migration can add them when Jifeng provides a distinct business code.
    /// </summary>
    public static IReadOnlyList<JifengPositionSourceExclusion> ExcludedSourceRows { get; } =
    [
        new("CV055", "Công nhân vận hành vát mép ống"),
        new("CV057", "Trợ lý chuyền đóng gói")
    ];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var seedValues = string.Join(
            ",\n            ",
            SeedRows.Select(BuildSeedValueSql));

        migrationBuilder.Sql($$"""
            ALTER TABLE public.positions
            ADD COLUMN IF NOT EXISTS "EmployeeCount" integer NOT NULL DEFAULT 0;

            LOCK TABLE public.positions IN SHARE MODE;

            CREATE TEMPORARY TABLE jifeng_position_seed
            (
                "Id" uuid NOT NULL,
                "Code" character varying(50) NOT NULL,
                "Name" character varying(200) NOT NULL,
                "Description" character varying(1000) NULL,
                "Status" integer NOT NULL,
                "EmployeeCount" integer NOT NULL,
                "CreatedAtUtc" timestamp without time zone NOT NULL,
                "UpdatedAtUtc" timestamp without time zone NULL
            ) ON COMMIT DROP;

            INSERT INTO jifeng_position_seed
            (
                "Id",
                "Code",
                "Name",
                "Description",
                "Status",
                "EmployeeCount",
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
                    FROM jifeng_position_seed
                    GROUP BY lower(btrim("Code"))
                    HAVING COUNT(*) > 1
                ) THEN
                    RAISE EXCEPTION 'Cannot seed Jifeng positions because the seed payload contains duplicate codes.';
                END IF;

                IF EXISTS
                (
                    SELECT 1
                    FROM public.positions AS existing
                    INNER JOIN jifeng_position_seed AS seed
                        ON lower(btrim(existing."Code")) = lower(btrim(seed."Code"))
                    WHERE existing."Id" <> seed."Id"
                ) THEN
                    RAISE EXCEPTION 'Cannot seed Jifeng positions because an existing position uses a conflicting code.';
                END IF;

                IF EXISTS
                (
                    SELECT 1
                    FROM public.positions AS existing
                    INNER JOIN jifeng_position_seed AS seed
                        ON existing."Id" = seed."Id"
                    WHERE lower(btrim(existing."Code")) <> lower(btrim(seed."Code"))
                ) THEN
                    RAISE EXCEPTION 'Cannot seed Jifeng positions because a seed identifier is already assigned to another code.';
                END IF;
            END
            $$;

            INSERT INTO public.positions
            (
                "Id",
                "Code",
                "Name",
                "Description",
                "Status",
                "EmployeeCount",
                "CreatedAtUtc",
                "UpdatedAtUtc"
            )
            SELECT
                "Id",
                "Code",
                "Name",
                "Description",
                "Status",
                "EmployeeCount",
                "CreatedAtUtc",
                "UpdatedAtUtc"
            FROM jifeng_position_seed
            ON CONFLICT ("Id") DO NOTHING;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Position rows can be referenced by employees and payroll mappings, so rollback must not delete operational data.
    }

    private static string BuildSeedValueSql(JifengPositionSeedRow row) =>
        $"('{row.Id:D}'::uuid, {ToSqlLiteral(row.Code)}, {ToSqlLiteral(row.Name)}, " +
        $"{ToSqlLiteral(row.Description)}, {row.Status}, {row.EmployeeCount}, " +
        $"{SeedTimestampSql}, NULL)";

    private static string ToSqlLiteral(string? value) =>
        value is null ? "NULL" : $"'{value.Replace("'", "''")}'";
}

public sealed record JifengPositionSeedRow(
    Guid Id,
    string Code,
    string Name,
    string? Description = null,
    int Status = 0,
    int EmployeeCount = 0);

public sealed record JifengPositionSourceExclusion(string Code, string Name);
