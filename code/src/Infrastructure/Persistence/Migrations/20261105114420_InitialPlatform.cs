using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace StableRail.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "platform");

            // Empty schemas reserve module boundaries. Financial tables and role grants belong to slice 3.
            migrationBuilder.EnsureSchema(name: "compliance");
            migrationBuilder.EnsureSchema(name: "finance");
            migrationBuilder.EnsureSchema(name: "identity");
            migrationBuilder.EnsureSchema(name: "monitoring");
            migrationBuilder.EnsureSchema(name: "payments");
            migrationBuilder.EnsureSchema(name: "providers");
            migrationBuilder.EnsureSchema(name: "webhooks");

            migrationBuilder.CreateTable(
                name: "module_ownership",
                schema: "platform",
                columns: table => new
                {
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    schema_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    responsibility = table.Column<string>(type: "text", nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_module_ownership", x => x.name);
                });

            migrationBuilder.InsertData(
                schema: "platform",
                table: "module_ownership",
                columns: new[] { "name", "recorded_at", "responsibility", "schema_name" },
                values: new object[,]
                {
                    { "compliance", new DateTimeOffset(new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Screening decisions, holds and case references.", "compliance" },
                    { "finance", new DateTimeOffset(new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Settlement evidence, reconciliation and statement imports.", "finance" },
                    { "identity", new DateTimeOffset(new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Tenants, merchants, operator accounts, sessions, MFA and environment-scoped credentials.", "identity" },
                    { "monitoring", new DateTimeOffset(new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Chain observations, confirmation tracking and reorganization evidence.", "monitoring" },
                    { "payments", new DateTimeOffset(new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Payment intents, options, instructions, idempotency and the acceptance lifecycle.", "payments" },
                    { "providers", new DateTimeOffset(new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Provider adapters, health, routing policy and route decisions.", "providers" },
                    { "webhooks", new DateTimeOffset(new DateTime(2026, 11, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Endpoints, signed delivery attempts and replays.", "webhooks" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "module_ownership",
                schema: "platform");

            migrationBuilder.DropSchema(name: "compliance");
            migrationBuilder.DropSchema(name: "finance");
            migrationBuilder.DropSchema(name: "identity");
            migrationBuilder.DropSchema(name: "monitoring");
            migrationBuilder.DropSchema(name: "payments");
            migrationBuilder.DropSchema(name: "providers");
            migrationBuilder.DropSchema(name: "webhooks");
        }
    }
}
