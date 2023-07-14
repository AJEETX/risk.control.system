using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace risk.control.system.Migrations
{
    /// <inheritdoc />
    public partial class Tat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Time2Update",
                table: "InvestigationTransaction",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Time2Update",
                table: "InvestigationTransaction");
        }
    }
}
