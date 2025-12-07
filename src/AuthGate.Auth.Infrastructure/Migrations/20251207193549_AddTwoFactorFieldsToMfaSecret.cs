using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthGate.Auth.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTwoFactorFieldsToMfaSecret : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EnabledAtUtc",
                table: "MfaSecrets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEnabled",
                table: "MfaSecrets",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUsedAtUtc",
                table: "MfaSecrets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryCodes",
                table: "MfaSecrets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecoveryCodesRemaining",
                table: "MfaSecrets",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnabledAtUtc",
                table: "MfaSecrets");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "MfaSecrets");

            migrationBuilder.DropColumn(
                name: "LastUsedAtUtc",
                table: "MfaSecrets");

            migrationBuilder.DropColumn(
                name: "RecoveryCodes",
                table: "MfaSecrets");

            migrationBuilder.DropColumn(
                name: "RecoveryCodesRemaining",
                table: "MfaSecrets");
        }
    }
}
