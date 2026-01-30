using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthGate.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingOrganizationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PendingOrganizationName",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PendingOrganizationPhone",
                table: "Users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingOrganizationName",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PendingOrganizationPhone",
                table: "Users");
        }
    }
}
