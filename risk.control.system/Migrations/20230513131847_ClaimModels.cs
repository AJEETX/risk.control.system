using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace risk.control.system.Migrations
{
    /// <inheritdoc />
    public partial class ClaimModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaimsInvestigationCaseId",
                table: "InvestigationServiceType",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimsInvestigationId",
                table: "FileAttachment",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Addressline",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClaimsInvestigation",
                columns: table => new
                {
                    ClaimsInvestigationCaseId = table.Column<string>(type: "TEXT", nullable: false),
                    ContractNumber = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    LineOfBusinessId = table.Column<string>(type: "TEXT", nullable: true),
                    InvestigationCaseStatusId = table.Column<string>(type: "TEXT", nullable: true),
                    ContractIssueDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CustomerName = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerDateOfBirth = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ContactNumber = table.Column<long>(type: "INTEGER", nullable: false),
                    ClaimType = table.Column<int>(type: "INTEGER", nullable: false),
                    DateOfIncident = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CauseOfLoss = table.Column<string>(type: "TEXT", nullable: true),
                    Gender = table.Column<int>(type: "INTEGER", nullable: false),
                    SumAssuredValue = table.Column<int>(type: "INTEGER", nullable: true),
                    PinCodeId = table.Column<string>(type: "TEXT", nullable: true),
                    StateId = table.Column<string>(type: "TEXT", nullable: true),
                    CountryId = table.Column<string>(type: "TEXT", nullable: false),
                    DistrictId = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerIncome = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomerOccupation = table.Column<string>(type: "TEXT", nullable: true),
                    CustomerEducation = table.Column<string>(type: "TEXT", nullable: true),
                    BeneficiaryName = table.Column<string>(type: "TEXT", nullable: true),
                    BeneficiaryRelationId = table.Column<string>(type: "TEXT", nullable: true),
                    BeneficiaryContactNumber = table.Column<int>(type: "INTEGER", nullable: true),
                    BeneficiaryIncome = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomerType = table.Column<int>(type: "INTEGER", nullable: false),
                    CostCentreId = table.Column<string>(type: "TEXT", nullable: true),
                    CaseEnablerId = table.Column<string>(type: "TEXT", nullable: true),
                    Comments = table.Column<string>(type: "TEXT", nullable: false),
                    Created = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Updated = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimsInvestigation", x => x.ClaimsInvestigationCaseId);
                    table.ForeignKey(
                        name: "FK_ClaimsInvestigation_BeneficiaryRelation_BeneficiaryRelationId",
                        column: x => x.BeneficiaryRelationId,
                        principalTable: "BeneficiaryRelation",
                        principalColumn: "BeneficiaryRelationId");
                    table.ForeignKey(
                        name: "FK_ClaimsInvestigation_CaseEnabler_CaseEnablerId",
                        column: x => x.CaseEnablerId,
                        principalTable: "CaseEnabler",
                        principalColumn: "CaseEnablerId");
                    table.ForeignKey(
                        name: "FK_ClaimsInvestigation_CostCentre_CostCentreId",
                        column: x => x.CostCentreId,
                        principalTable: "CostCentre",
                        principalColumn: "CostCentreId");
                    table.ForeignKey(
                        name: "FK_ClaimsInvestigation_Country_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Country",
                        principalColumn: "CountryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimsInvestigation_District_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "District",
                        principalColumn: "DistrictId");
                    table.ForeignKey(
                        name: "FK_ClaimsInvestigation_InvestigationCaseStatus_InvestigationCaseStatusId",
                        column: x => x.InvestigationCaseStatusId,
                        principalTable: "InvestigationCaseStatus",
                        principalColumn: "InvestigationCaseStatusId");
                    table.ForeignKey(
                        name: "FK_ClaimsInvestigation_LineOfBusiness_LineOfBusinessId",
                        column: x => x.LineOfBusinessId,
                        principalTable: "LineOfBusiness",
                        principalColumn: "LineOfBusinessId");
                    table.ForeignKey(
                        name: "FK_ClaimsInvestigation_PinCode_PinCodeId",
                        column: x => x.PinCodeId,
                        principalTable: "PinCode",
                        principalColumn: "PinCodeId");
                    table.ForeignKey(
                        name: "FK_ClaimsInvestigation_State_StateId",
                        column: x => x.StateId,
                        principalTable: "State",
                        principalColumn: "StateId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvestigationServiceType_ClaimsInvestigationCaseId",
                table: "InvestigationServiceType",
                column: "ClaimsInvestigationCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_FileAttachment_ClaimsInvestigationId",
                table: "FileAttachment",
                column: "ClaimsInvestigationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimsInvestigation_BeneficiaryRelationId",
                table: "ClaimsInvestigation",
                column: "BeneficiaryRelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimsInvestigation_CaseEnablerId",
                table: "ClaimsInvestigation",
                column: "CaseEnablerId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimsInvestigation_CostCentreId",
                table: "ClaimsInvestigation",
                column: "CostCentreId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimsInvestigation_CountryId",
                table: "ClaimsInvestigation",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimsInvestigation_DistrictId",
                table: "ClaimsInvestigation",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimsInvestigation_InvestigationCaseStatusId",
                table: "ClaimsInvestigation",
                column: "InvestigationCaseStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimsInvestigation_LineOfBusinessId",
                table: "ClaimsInvestigation",
                column: "LineOfBusinessId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimsInvestigation_PinCodeId",
                table: "ClaimsInvestigation",
                column: "PinCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_ClaimsInvestigation_StateId",
                table: "ClaimsInvestigation",
                column: "StateId");

            migrationBuilder.AddForeignKey(
                name: "FK_FileAttachment_ClaimsInvestigation_ClaimsInvestigationId",
                table: "FileAttachment",
                column: "ClaimsInvestigationId",
                principalTable: "ClaimsInvestigation",
                principalColumn: "ClaimsInvestigationCaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvestigationServiceType_ClaimsInvestigation_ClaimsInvestigationCaseId",
                table: "InvestigationServiceType",
                column: "ClaimsInvestigationCaseId",
                principalTable: "ClaimsInvestigation",
                principalColumn: "ClaimsInvestigationCaseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FileAttachment_ClaimsInvestigation_ClaimsInvestigationId",
                table: "FileAttachment");

            migrationBuilder.DropForeignKey(
                name: "FK_InvestigationServiceType_ClaimsInvestigation_ClaimsInvestigationCaseId",
                table: "InvestigationServiceType");

            migrationBuilder.DropTable(
                name: "ClaimsInvestigation");

            migrationBuilder.DropIndex(
                name: "IX_InvestigationServiceType_ClaimsInvestigationCaseId",
                table: "InvestigationServiceType");

            migrationBuilder.DropIndex(
                name: "IX_FileAttachment_ClaimsInvestigationId",
                table: "FileAttachment");

            migrationBuilder.DropColumn(
                name: "ClaimsInvestigationCaseId",
                table: "InvestigationServiceType");

            migrationBuilder.DropColumn(
                name: "ClaimsInvestigationId",
                table: "FileAttachment");

            migrationBuilder.DropColumn(
                name: "Addressline",
                table: "AspNetUsers");
        }
    }
}
