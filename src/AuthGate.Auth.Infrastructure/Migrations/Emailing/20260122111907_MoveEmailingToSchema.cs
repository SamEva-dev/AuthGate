using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthGate.Auth.Infrastructure.Migrations.Emailing
{
    /// <inheritdoc />
    public partial class MoveEmailingToSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "emailing");

            migrationBuilder.RenameTable(
                name: "EmailMessages",
                newName: "EmailMessages",
                newSchema: "emailing");

            migrationBuilder.RenameTable(
                name: "EmailEvents",
                newName: "EmailEvents",
                newSchema: "emailing");

            migrationBuilder.RenameTable(
                name: "EmailAttachments",
                newName: "EmailAttachments",
                newSchema: "emailing");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "EmailMessages",
                schema: "emailing",
                newName: "EmailMessages");

            migrationBuilder.RenameTable(
                name: "EmailEvents",
                schema: "emailing",
                newName: "EmailEvents");

            migrationBuilder.RenameTable(
                name: "EmailAttachments",
                schema: "emailing",
                newName: "EmailAttachments");
        }
    }
}
