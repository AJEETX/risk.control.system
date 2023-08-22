using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace risk.control.system.Migrations
{
    /// <inheritdoc />
    public partial class Notes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClaimMessage_CaseLocation_CaseLocationId",
                table: "ClaimMessage");

            migrationBuilder.DropIndex(
                name: "IX_ClaimMessage_CaseLocationId",
                table: "ClaimMessage");

            migrationBuilder.DropColumn(
                name: "CaseLocationId",
                table: "ClaimMessage");

            migrationBuilder.AddColumn<string>(
                name: "CurrentClaimOwner",
                table: "ClaimsInvestigation",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorId",
                table: "ClaimReport",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClaimNote",
                columns: table => new
                {
                    ClaimNoteId = table.Column<string>(type: "TEXT", nullable: false),
                    Sender = table.Column<string>(type: "TEXT", nullable: false),
                    Comment = table.Column<string>(type: "TEXT", nullable: false),
                    ParentClaimNoteClaimNoteId = table.Column<string>(type: "TEXT", nullable: true),
                    ClaimsInvestigationId = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimNote", x => x.ClaimNoteId);
                    table.ForeignKey(
                        name: "FK_ClaimNote_ClaimNote_ParentClaimNoteClaimNoteId",
                        column: x => x.ParentClaimNoteClaimNoteId,
                        principalTable: "ClaimNote",
                        principalColumn: "ClaimNoteId");
                    table.ForeignKey(
                        name: "FK_ClaimNote_ClaimsInvestigation_ClaimsInvestigationId",
                        column: x => x.ClaimsInvestigationId,
                        principalTable: "ClaimsInvestigation",
                        principalColumn: "ClaimsInvestigationId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimReport_VendorId",
                table: "ClaimReport",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimMessage_ClaimsInvestigationId",
                table: "ClaimMessage",
                column: "ClaimsInvestigationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimNote_ClaimsInvestigationId",
                table: "ClaimNote",
                column: "ClaimsInvestigationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimNote_ParentClaimNoteClaimNoteId",
                table: "ClaimNote",
                column: "ParentClaimNoteClaimNoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimMessage_ClaimsInvestigation_ClaimsInvestigationId",
                table: "ClaimMessage",
                column: "ClaimsInvestigationId",
                principalTable: "ClaimsInvestigation",
                principalColumn: "ClaimsInvestigationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimReport_Vendor_VendorId",
                table: "ClaimReport",
                column: "VendorId",
                principalTable: "Vendor",
                principalColumn: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClaimMessage_ClaimsInvestigation_ClaimsInvestigationId",
                table: "ClaimMessage");

            migrationBuilder.DropForeignKey(
                name: "FK_ClaimReport_Vendor_VendorId",
                table: "ClaimReport");

            migrationBuilder.DropTable(
                name: "ClaimNote");

            migrationBuilder.DropIndex(
                name: "IX_ClaimReport_VendorId",
                table: "ClaimReport");

            migrationBuilder.DropIndex(
                name: "IX_ClaimMessage_ClaimsInvestigationId",
                table: "ClaimMessage");

            migrationBuilder.DropColumn(
                name: "CurrentClaimOwner",
                table: "ClaimsInvestigation");

            migrationBuilder.DropColumn(
                name: "VendorId",
                table: "ClaimReport");

            migrationBuilder.AddColumn<long>(
                name: "CaseLocationId",
                table: "ClaimMessage",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaimMessage_CaseLocationId",
                table: "ClaimMessage",
                column: "CaseLocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ClaimMessage_CaseLocation_CaseLocationId",
                table: "ClaimMessage",
                column: "CaseLocationId",
                principalTable: "CaseLocation",
                principalColumn: "CaseLocationId");
        }
    }
}
