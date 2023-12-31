using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace risk.control.system.Migrations
{
    /// <inheritdoc />
    public partial class invoiceprint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvestigationServiceTypeId",
                table: "VendorInvoice",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendorInvoice_InvestigationServiceTypeId",
                table: "VendorInvoice",
                column: "InvestigationServiceTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_VendorInvoice_InvestigationServiceType_InvestigationServiceTypeId",
                table: "VendorInvoice",
                column: "InvestigationServiceTypeId",
                principalTable: "InvestigationServiceType",
                principalColumn: "InvestigationServiceTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VendorInvoice_InvestigationServiceType_InvestigationServiceTypeId",
                table: "VendorInvoice");

            migrationBuilder.DropIndex(
                name: "IX_VendorInvoice_InvestigationServiceTypeId",
                table: "VendorInvoice");

            migrationBuilder.DropColumn(
                name: "InvestigationServiceTypeId",
                table: "VendorInvoice");
        }
    }
}
