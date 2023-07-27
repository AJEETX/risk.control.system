using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace risk.control.system.Migrations
{
    /// <inheritdoc />
    public partial class SuperDoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClaimsInvestigation_InvestigationCaseStatus_InvestigationCaseStatusId",
                table: "ClaimsInvestigation");

            migrationBuilder.DropForeignKey(
                name: "FK_ClaimsInvestigation_InvestigationCaseSubStatus_InvestigationCaseSubStatusId",
                table: "ClaimsInvestigation");

            migrationBuilder.AlterColumn<string>(
                name: "InvestigationCaseSubStatusId",
                table: "ClaimsInvestigation",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "InvestigationCaseStatusId",
                table: "ClaimsInvestigation",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<byte[]>(
                name: "SupervisorPicture",
                table: "ClaimReport",
                type: "BLOB",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimsInvestigation_InvestigationCaseStatus_InvestigationCaseStatusId",
                table: "ClaimsInvestigation",
                column: "InvestigationCaseStatusId",
                principalTable: "InvestigationCaseStatus",
                principalColumn: "InvestigationCaseStatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimsInvestigation_InvestigationCaseSubStatus_InvestigationCaseSubStatusId",
                table: "ClaimsInvestigation",
                column: "InvestigationCaseSubStatusId",
                principalTable: "InvestigationCaseSubStatus",
                principalColumn: "InvestigationCaseSubStatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClaimsInvestigation_InvestigationCaseStatus_InvestigationCaseStatusId",
                table: "ClaimsInvestigation");

            migrationBuilder.DropForeignKey(
                name: "FK_ClaimsInvestigation_InvestigationCaseSubStatus_InvestigationCaseSubStatusId",
                table: "ClaimsInvestigation");

            migrationBuilder.DropColumn(
                name: "SupervisorPicture",
                table: "ClaimReport");

            migrationBuilder.AlterColumn<string>(
                name: "InvestigationCaseSubStatusId",
                table: "ClaimsInvestigation",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InvestigationCaseStatusId",
                table: "ClaimsInvestigation",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimsInvestigation_InvestigationCaseStatus_InvestigationCaseStatusId",
                table: "ClaimsInvestigation",
                column: "InvestigationCaseStatusId",
                principalTable: "InvestigationCaseStatus",
                principalColumn: "InvestigationCaseStatusId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimsInvestigation_InvestigationCaseSubStatus_InvestigationCaseSubStatusId",
                table: "ClaimsInvestigation",
                column: "InvestigationCaseSubStatusId",
                principalTable: "InvestigationCaseSubStatus",
                principalColumn: "InvestigationCaseSubStatusId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
