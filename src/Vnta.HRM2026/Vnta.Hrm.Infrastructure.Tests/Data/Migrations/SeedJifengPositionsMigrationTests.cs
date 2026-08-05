using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Vnta.Hrm.Infrastructure.Data.Migrations;
using Xunit;

namespace Vnta.Hrm.Infrastructure.Tests.Data.Migrations;

public sealed class SeedJifengPositionsMigrationTests
{
    [Fact]
    public void Seed_rows_are_a_valid_unique_code_catalogue_from_the_source_sheet()
    {
        Assert.Equal(102, SeedJifengPositions.SeedRows.Count);
        Assert.Equal(
            SeedJifengPositions.SeedRows.Count,
            SeedJifengPositions.SeedRows
                .Select(row => row.Id)
                .Distinct()
                .Count());
        Assert.Equal(
            SeedJifengPositions.SeedRows.Count,
            SeedJifengPositions.SeedRows
                .Select(row => row.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());

        Assert.Contains(
            SeedJifengPositions.SeedRows,
            row => row.Code == "CV055" && row.Name == "Nhân viên");
        Assert.Contains(
            SeedJifengPositions.SeedRows,
            row => row.Code == "CV057" && row.Name == "Công nhân kiểm hàng");
        Assert.Contains(
            SeedJifengPositions.SeedRows,
            row => row.Code == "CD051" && row.Name == "Trợ lý chuyền đóng gói");

        Assert.Equal(
            [
                new JifengPositionSourceExclusion("CV055", "Công nhân vận hành vát mép ống"),
                new JifengPositionSourceExclusion("CV057", "Trợ lý chuyền đóng gói")
            ],
            SeedJifengPositions.ExcludedSourceRows);

        Assert.All(SeedJifengPositions.SeedRows, row =>
        {
            Assert.False(string.IsNullOrWhiteSpace(row.Code));
            Assert.False(string.IsNullOrWhiteSpace(row.Name));
            Assert.InRange(row.Code.Length, 1, 50);
            Assert.InRange(row.Name.Length, 1, 200);
            Assert.Null(row.Description);
            Assert.Equal(0, row.Status);
            Assert.Equal(0, row.EmployeeCount);
        });
    }

    [Fact]
    public void Up_uses_a_locked_idempotent_insert_with_code_collision_preflight()
    {
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        new ExposedSeedJifengPositions().BuildUp(builder);

        var sql = string.Join(
            Environment.NewLine,
            builder.Operations
                .OfType<SqlOperation>()
                .Select(operation => operation.Sql));

        Assert.Contains("ADD COLUMN IF NOT EXISTS \"EmployeeCount\"", sql, StringComparison.Ordinal);
        Assert.Contains("LOCK TABLE public.positions IN SHARE MODE", sql, StringComparison.Ordinal);
        Assert.Contains("lower(btrim(existing.\"Code\"))", sql, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT (\"Id\") DO NOTHING", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ON CONFLICT (\"Code\")", sql, StringComparison.Ordinal);
    }

    private sealed class ExposedSeedJifengPositions : SeedJifengPositions
    {
        public void BuildUp(MigrationBuilder migrationBuilder) => Up(migrationBuilder);
    }
}
