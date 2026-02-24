using risk.control.system.Models;

namespace risk.control.system.Seeds
{
    public static class ClientCompanySetupSeed
    {
        public static async Task Seed(ApplicationDbContext context)
        {
            #region BENEFICIARY-RELATION

            var brother = new BeneficiaryRelation
            {
                Name = "BROTHER",
                Code = "BROTHER",
                Updated = DateTime.UtcNow,
            };
            var brotherEntity = await context.AddAsync(brother);

            var father = new BeneficiaryRelation
            {
                Name = "FATHER",
                Code = "FATHER",
                Updated = DateTime.UtcNow,
            };
            var fatherEntity = await context.AddAsync(father);

            var mother = new BeneficiaryRelation
            {
                Name = "MOTHER",
                Code = "MOTHER",
                Updated = DateTime.UtcNow,
            };
            var motherEntity = await context.AddAsync(mother);


            var sister = new BeneficiaryRelation
            {
                Name = "SISTER",
                Code = "SISTER",
                Updated = DateTime.UtcNow,
            };
            var sisterEntity = await context.AddAsync(sister);

            var uncle = new BeneficiaryRelation
            {
                Name = "UNCLE",
                Code = "UNCLE",
                Updated = DateTime.UtcNow,
            };
            var uncleEntity = await context.AddAsync(uncle);

            var aunty = new BeneficiaryRelation
            {
                Name = "AUNTY",
                Code = "AUNTY",
                Updated = DateTime.UtcNow,
            };
            var auntyEntity = await context.AddAsync(aunty);

            var newphew = new BeneficiaryRelation
            {
                Name = "NEWPHEW",
                Code = "NEWPHEW",
                Updated = DateTime.UtcNow,
            };
            var newphewEntity = await context.AddAsync(newphew);

            var niece = new BeneficiaryRelation
            {
                Name = "NIECE",
                Code = "NIECE",
                Updated = DateTime.UtcNow,
            };
            var nieceEntity = await context.AddAsync(niece);

            var inlaw = new BeneficiaryRelation
            {
                Name = "INLAW",
                Code = "INLAW",
                Updated = DateTime.UtcNow,
            };
            var inlawEntity = await context.AddAsync(inlaw);

            var unknown = new BeneficiaryRelation
            {
                Name = "UNKNOWN",
                Code = "UNKNOWN",
                Updated = DateTime.UtcNow,
            };
            var unknownEntity = await context.AddAsync(unknown);

            #endregion

            #region CASE ENABLER
            var demoCaseEnabler = new CaseEnabler
            {
                Name = "DEMO",
                Code = "DEMO",
                Updated = DateTime.UtcNow,
            };
            var demoCaseEnablerEntity = await context.CaseEnabler.AddAsync(demoCaseEnabler);

            var doubtCaseEnabler = new CaseEnabler
            {
                Name = "DOUBT-DETAILS",
                Code = "DOUBT",
                Updated = DateTime.UtcNow,
            };
            var doubtCaseEnablerEntity = await context.CaseEnabler.AddAsync(doubtCaseEnabler);

            var highAmountCaseEnabler = new CaseEnabler
            {
                Name = "HIGH-PREMIUM",
                Code = "HIGH",
                Updated = DateTime.UtcNow,
            };
            var highAmountCaseEnablerEntity = await context.CaseEnabler.AddAsync(highAmountCaseEnabler);

            var unknownCaseEnabler = new CaseEnabler
            {
                Name = "UNKNOWN",
                Code = "UNKNOWN",
                Updated = DateTime.UtcNow,
            };
            var unknownCaseEnablerCaseEnablerEntity = await context.CaseEnabler.AddAsync(unknownCaseEnabler);
            #endregion

            #region COST CENTRE

            var demoCentre = new CostCentre
            {
                Name = "DEMO",
                Code = "DEMO",
                Updated = DateTime.UtcNow,
            };

            var demoCentreEntity = await context.CostCentre.AddAsync(demoCentre);

            var loansCostCentre = new CostCentre
            {
                Name = "LOANS",
                Code = "LOANS",
                Updated = DateTime.UtcNow,
            };

            var loansCostCentreEntity = await context.CostCentre.AddAsync(loansCostCentre);

            var financeCostCentre = new CostCentre
            {
                Name = "FINANCE",
                Code = "FINANCE",
                Updated = DateTime.UtcNow,
            };

            var financeCostCentreEntity = await context.CostCentre.AddAsync(financeCostCentre);

            var unknownCostCentre = new CostCentre
            {
                Name = "UNKNOWN",
                Code = "UNKNOWN",
                Updated = DateTime.UtcNow,
            };

            var unknownCostCentreEntity = await context.CostCentre.AddAsync(unknownCostCentre);
            #endregion

        }
    }
}
