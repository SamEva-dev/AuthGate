using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthGate.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DDD_UserInvitation_TokenHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserInvitations_Token",
                table: "UserInvitations");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "UserInvitations");

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "UserInvitations",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_TokenHash",
                table: "UserInvitations",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserInvitations_TokenHash",
                table: "UserInvitations");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "UserInvitations");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "UserInvitations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserInvitations_Token",
                table: "UserInvitations",
                column: "Token",
                unique: true);
        }
    }
}
