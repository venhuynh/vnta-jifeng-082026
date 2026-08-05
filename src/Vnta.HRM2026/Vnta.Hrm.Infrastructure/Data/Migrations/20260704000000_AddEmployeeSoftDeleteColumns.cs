using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Vnta.Hrm.Infrastructure.Data;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260704000000_AddEmployeeSoftDeleteColumns")]
    public partial class AddEmployeeSoftDeleteColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE public.employees
                ADD COLUMN IF NOT EXISTS "IsDeleted" boolean NOT NULL DEFAULT FALSE;

                ALTER TABLE public.employees
                ADD COLUMN IF NOT EXISTS "DeletedAtUtc" timestamp without time zone NULL;

                CREATE INDEX IF NOT EXISTS "IX_employees_IsDeleted"
                ON public.employees ("IsDeleted");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS public."IX_employees_IsDeleted";

                ALTER TABLE public.employees
                DROP COLUMN IF EXISTS "IsDeleted";

                ALTER TABLE public.employees
                DROP COLUMN IF EXISTS "DeletedAtUtc";
                """);
        }
    }
}
