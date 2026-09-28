using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace clinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddFailedAttemptsToOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedAttempts",
                table: "ApplicationUserOTPs",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedAttempts",
                table: "ApplicationUserOTPs");
        }
    }
}
