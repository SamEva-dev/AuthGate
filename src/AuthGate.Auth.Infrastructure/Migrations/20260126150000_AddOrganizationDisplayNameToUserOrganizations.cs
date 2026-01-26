using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthGate.Auth.Infrastructure.Migrations;

public partial class AddOrganizationDisplayNameToUserOrganizations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "OrganizationDisplayName",
            table: "UserOrganizations",
            type: "character varying(256)",
            maxLength: 256,
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_UserOrganizations_OrganizationId_UserId",
            table: "UserOrganizations",
            columns: new[] { "OrganizationId", "UserId" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_UserOrganizations_OrganizationId_UserId",
            table: "UserOrganizations");

        migrationBuilder.DropColumn(
            name: "OrganizationDisplayName",
            table: "UserOrganizations");
    }
}
