using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vnta.Hrm.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "events",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_id = table.Column<string>(type: "text", nullable: false),
                    actor_display_name = table.Column<string>(type: "text", nullable: false),
                    actor_kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity_id = table.Column<string>(type: "text", nullable: true),
                    entity_display_name = table.Column<string>(type: "text", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    operation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: true),
                    schema_version = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "property_changes",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    property_label = table.Column<string>(type: "text", nullable: false),
                    old_value_json = table.Column<string>(type: "jsonb", nullable: true),
                    new_value_json = table.Column<string>(type: "jsonb", nullable: true),
                    old_display = table.Column<string>(type: "text", nullable: true),
                    new_display = table.Column<string>(type: "text", nullable: true),
                    is_sensitive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    old_ciphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    new_ciphertext = table.Column<byte[]>(type: "bytea", nullable: true),
                    encryption_key_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_property_changes", x => x.id);
                    table.ForeignKey(
                        name: "FK_property_changes_events_audit_event_id",
                        column: x => x.audit_event_id,
                        principalSchema: "audit",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_action_occurred_at_utc_desc",
                schema: "audit",
                table: "events",
                columns: new[] { "action", "occurred_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_actor_occurred_at_utc_desc",
                schema: "audit",
                table: "events",
                columns: new[] { "actor_id", "occurred_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_correlation_id",
                schema: "audit",
                table: "events",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_entity_occurred_at_utc_desc",
                schema: "audit",
                table: "events",
                columns: new[] { "entity_type", "entity_id", "occurred_at_utc" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_occurred_at_utc_desc",
                schema: "audit",
                table: "events",
                column: "occurred_at_utc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_operation_id",
                schema: "audit",
                table: "events",
                column: "operation_id");

            migrationBuilder.CreateIndex(
                name: "ux_audit_events_event_key",
                schema: "audit",
                table: "events",
                column: "event_key",
                unique: true,
                filter: "\"event_key\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_audit_property_changes_audit_event_id",
                schema: "audit",
                table: "property_changes",
                column: "audit_event_id");

            // PostgreSQL exposes xmin as a system column. The model maps it as a concurrency
            // token; no physical column needs to be created for the four pilot entities.
            migrationBuilder.Sql(
                """
                CREATE FUNCTION audit.reject_mutation()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                BEGIN
                    RAISE EXCEPTION 'Audit records are append-only.' USING ERRCODE = '55000';
                END;
                $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER trg_events_append_only
                BEFORE UPDATE OR DELETE ON audit.events
                FOR EACH ROW EXECUTE FUNCTION audit.reject_mutation();

                CREATE TRIGGER trg_property_changes_append_only
                BEFORE UPDATE OR DELETE ON audit.property_changes
                FOR EACH ROW EXECUTE FUNCTION audit.reject_mutation();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "property_changes",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "events",
                schema: "audit");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS audit.reject_mutation();");
        }
    }
}
