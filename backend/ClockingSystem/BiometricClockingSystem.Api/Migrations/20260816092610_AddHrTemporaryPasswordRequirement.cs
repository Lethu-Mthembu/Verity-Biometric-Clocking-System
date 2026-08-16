using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BiometricClockingSystem.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddHrTemporaryPasswordRequirement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequirePasswordChange",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequirePasswordChange",
                table: "Users");
        }
    }
}
