using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace risk.control.system.Migrations
{
    /// <inheritdoc />
    public partial class invoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VendorInvoice",
                columns: table => new
                {
                    VendorInvoiceId = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "TEXT", nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DueDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VendorId = table.Column<string>(type: "TEXT", nullable: true),
                    ClientCompanyId = table.Column<string>(type: "TEXT", nullable: true),
                    NoteToRecipient = table.Column<string>(type: "TEXT", nullable: false),
                    SubTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "TEXT", nullable: false),
                    ClaimReportId = table.Column<string>(type: "TEXT", nullable: true),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendorInvoice", x => x.VendorInvoiceId);
                    table.ForeignKey(
                        name: "FK_VendorInvoice_ClaimReport_ClaimReportId",
                        column: x => x.ClaimReportId,
                        principalTable: "ClaimReport",
                        principalColumn: "ClaimReportId");
                    table.ForeignKey(
                        name: "FK_VendorInvoice_ClientCompany_ClientCompanyId",
                        column: x => x.ClientCompanyId,
                        principalTable: "ClientCompany",
                        principalColumn: "ClientCompanyId");
                    table.ForeignKey(
                        name: "FK_VendorInvoice_Vendor_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendor",
                        principalColumn: "VendorId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvoice_ClaimReportId",
                table: "VendorInvoice",
                column: "ClaimReportId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvoice_ClientCompanyId",
                table: "VendorInvoice",
                column: "ClientCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvoice_VendorId",
                table: "VendorInvoice",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VendorInvoice");
        }
    }
}
