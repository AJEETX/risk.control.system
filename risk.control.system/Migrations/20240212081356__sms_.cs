using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace risk.control.system.Migrations
{
    /// <inheritdoc />
    public partial class _sms_ : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PreviousClaimMessageId",
                table: "ClaimMessage",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecepicientPhone",
                table: "ClaimMessage",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScheduleTime",
                table: "ClaimMessage",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SenderPhone",
                table: "ClaimMessage",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousClaimMessageId",
                table: "ClaimMessage");

            migrationBuilder.DropColumn(
                name: "RecepicientPhone",
                table: "ClaimMessage");

            migrationBuilder.DropColumn(
                name: "ScheduleTime",
                table: "ClaimMessage");

            migrationBuilder.DropColumn(
                name: "SenderPhone",
                table: "ClaimMessage");
        }
    }
}
