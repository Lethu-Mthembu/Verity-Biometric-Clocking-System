using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace BiometricClockingSystem.Api.Migrations;

[DbContext(typeof(Data.ApplicationDbContext))]
[Migration("20260816150000_AddP1SecurityHardening")]
public partial class AddP1SecurityHardening : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "FailedLoginCount",
            table: "Users",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        migrationBuilder.AddColumn<DateTime>(
            name: "LockoutEndUtc",
            table: "Users",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "SecurityStamp",
            table: "Users",
            type: "character varying(64)",
            maxLength: 64,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateTable(
            name: "AuditLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                ActorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TargetType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                TargetId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                ClientIpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AuditLogs", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_OccurredAt",
            table: "AuditLogs",
            column: "OccurredAt");

        migrationBuilder.CreateIndex(
            name: "IX_AuditLogs_TargetType_TargetId",
            table: "AuditLogs",
            columns: new[] { "TargetType", "TargetId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AuditLogs");
        migrationBuilder.DropColumn(name: "FailedLoginCount", table: "Users");
        migrationBuilder.DropColumn(name: "LockoutEndUtc", table: "Users");
        migrationBuilder.DropColumn(name: "SecurityStamp", table: "Users");
    }
}
