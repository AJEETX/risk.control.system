using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace risk.control.system.Migrations
{
    /// <inheritdoc />
    public partial class Init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RawMessage",
                table: "TrashMessage",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawMessage",
                table: "SentMessage",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawMessage",
                table: "InboxMessage",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RawMessage",
                table: "DeletedMessage",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RawMessage",
                table: "TrashMessage");

            migrationBuilder.DropColumn(
                name: "RawMessage",
                table: "SentMessage");

            migrationBuilder.DropColumn(
                name: "RawMessage",
                table: "InboxMessage");

            migrationBuilder.DropColumn(
                name: "RawMessage",
                table: "DeletedMessage");
        }
    }
}
