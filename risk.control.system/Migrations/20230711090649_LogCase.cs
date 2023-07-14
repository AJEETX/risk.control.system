using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace risk.control.system.Migrations
{
    /// <inheritdoc />
    public partial class LogCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvestigationTransaction",
                columns: table => new
                {
                    InvestigationTransactionId = table.Column<string>(type: "TEXT", nullable: false),
                    ClaimsInvestigationId = table.Column<string>(type: "TEXT", nullable: true),
                    InvestigationCaseStatusId = table.Column<string>(type: "TEXT", nullable: true),
                    InvestigationCaseSubStatusId = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestigationTransaction", x => x.InvestigationTransactionId);
                    table.ForeignKey(
                        name: "FK_InvestigationTransaction_ClaimsInvestigation_ClaimsInvestigationId",
                        column: x => x.ClaimsInvestigationId,
                        principalTable: "ClaimsInvestigation",
                        principalColumn: "ClaimsInvestigationId");
                    table.ForeignKey(
                        name: "FK_InvestigationTransaction_InvestigationCaseStatus_InvestigationCaseStatusId",
                        column: x => x.InvestigationCaseStatusId,
                        principalTable: "InvestigationCaseStatus",
                        principalColumn: "InvestigationCaseStatusId");
                    table.ForeignKey(
                        name: "FK_InvestigationTransaction_InvestigationCaseSubStatus_InvestigationCaseSubStatusId",
                        column: x => x.InvestigationCaseSubStatusId,
                        principalTable: "InvestigationCaseSubStatus",
                        principalColumn: "InvestigationCaseSubStatusId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvestigationTransaction_ClaimsInvestigationId",
                table: "InvestigationTransaction",
                column: "ClaimsInvestigationId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestigationTransaction_InvestigationCaseStatusId",
                table: "InvestigationTransaction",
                column: "InvestigationCaseStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_InvestigationTransaction_InvestigationCaseSubStatusId",
                table: "InvestigationTransaction",
                column: "InvestigationCaseSubStatusId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestigationTransaction");
        }
    }
}
