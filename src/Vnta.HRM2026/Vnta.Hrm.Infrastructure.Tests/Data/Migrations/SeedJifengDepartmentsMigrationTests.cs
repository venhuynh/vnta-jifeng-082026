using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Vnta.Hrm.Infrastructure.Data.Migrations;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.Data.Migrations;

public sealed class SeedJifengDepartmentsMigrationTests
{
    [Fact]
    public void Seed_rows_match_the_approved_level_one_and_level_two_structure()
    {
        var expected = new[]
        {
            "PB005|Hành Chính Nhân Sự/ Tuân thủ|Hành Chính Nhân Sự/ Tuân thủ",
            "02|Hành Chính Nhân Sự/ Tuân thủ|An toàn và tuân thủ",
            "04|Hành Chính Nhân Sự/ Tuân thủ|Bảo vệ",
            "01|Hành Chính Nhân Sự/ Tuân thủ|Hành chính nhân sự",
            "05|Hành Chính Nhân Sự/ Tuân thủ|Nhà ăn",
            "03|Hành Chính Nhân Sự/ Tuân thủ|Tạp vụ",
            "YT|Hành Chính Nhân Sự/ Tuân thủ|Y tế",
            "KT|Kế toán|Kế toán",
            "PB001|Mặc định|Mặc định",
            "PB002|Phát Triển Sản Phẩm/ Quản Lý Chất Lượng|Phát Triển Sản Phẩm/ Quản Lý Chất Lượng",
            "CL|Phát Triển Sản Phẩm/ Quản Lý Chất Lượng|Chất lượng",
            "KP|Phát Triển Sản Phẩm/ Quản Lý Chất Lượng|Khai phá",
            "PB017|Phòng sản xuất/生产|Phòng sản xuất/生产",
            "SX|Sản Xuất|Sản Xuất",
            "BTKT|Sản Xuất|Bảo trì/ Kỹ thuật",
            "JF/PACK|Sản Xuất|Đóng gói",
            "GC|Sản Xuất|Gia công",
            "KH|Sản Xuất|Kế hoạch",
            "TM|Sản Xuất|Thu mua",
            "TK|Sản Xuất|Văn phòng xưởng",
            "JF/BOD|Ban giám đốc điều hành|Ban giám đốc điều hành",
            "JF/LOG|Xuất Nhập Khẩu / Sales|Xuất Nhập Khẩu / Sales",
            "JF/WH|Xuất Nhập Khẩu / Sales|Kho",
            "LXN|Xuất Nhập Khẩu / Sales|Lái xe nâng",
            "SL|Xuất Nhập Khẩu / Sales|Sales",
            "JF/IM|Xuất Nhập Khẩu / Sales|Xuất nhập khẩu"
        };

        var actual = SeedJifengDepartments.SeedRows
            .Select(row => $"{row.Code}|{row.CenterName}|{row.DepartmentOrWorkshopName}")
            .ToArray();

        Assert.Equal(expected, actual);
        Assert.Equal(26, SeedJifengDepartments.SeedRows.Count);
        Assert.Equal(
            SeedJifengDepartments.SeedRows.Count,
            SeedJifengDepartments.SeedRows
                .Select(row => row.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
        Assert.All(SeedJifengDepartments.SeedRows, row =>
        {
            Assert.Null(row.TeamName);
            Assert.Null(row.GroupName);
            Assert.Equal(0, row.Status);
            Assert.NotEqual("Jifeng", row.CenterName);
        });
    }

    [Fact]
    public void Up_uses_a_locked_idempotent_insert_with_code_collision_preflight()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedSeedJifengDepartments().BuildUp(builder);

        var sql = string.Join(
            Environment.NewLine,
            builder.Operations
                .OfType<SqlOperation>()
                .Select(operation => operation.Sql));

        Assert.Contains("LOCK TABLE public.departments IN SHARE MODE", sql, StringComparison.Ordinal);
        Assert.Contains("lower(btrim(existing.\"Code\"))", sql, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT (\"Id\") DO NOTHING", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ON CONFLICT (\"Code\")", sql, StringComparison.Ordinal);
    }

    private sealed class ExposedSeedJifengDepartments : SeedJifengDepartments
    {
        public void BuildUp(MigrationBuilder migrationBuilder) => Up(migrationBuilder);
    }
}
