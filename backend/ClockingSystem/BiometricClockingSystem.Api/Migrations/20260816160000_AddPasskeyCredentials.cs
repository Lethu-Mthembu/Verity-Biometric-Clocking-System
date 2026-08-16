using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiometricClockingSystem.Api.Migrations;

[DbContext(typeof(Data.ApplicationDbContext))]
[Migration("20260816160000_AddPasskeyCredentials")]
public partial class AddPasskeyCredentials : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PasskeyCredentials",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CredentialId = table.Column<byte[]>(type: "bytea", nullable: false),
                PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                UserHandle = table.Column<byte[]>(type: "bytea", nullable: false),
                SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PasskeyCredentials", x => x.Id);
                table.ForeignKey(
                    name: "FK_PasskeyCredentials_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_PasskeyCredentials_CredentialId",
            table: "PasskeyCredentials",
            column: "CredentialId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_PasskeyCredentials_UserId",
            table: "PasskeyCredentials",
            column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(name: "PasskeyCredentials");
}
