using risk.control.system.Data;
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
                Updated = DateTime.Now,
            };
            var brotherEntity = await context.AddAsync(brother);

            var father = new BeneficiaryRelation
            {
                Name = "FATHER",
                Code = "FATHER",
                Updated = DateTime.Now,
            };
            var fatherEntity = await context.AddAsync(father);

            var mother = new BeneficiaryRelation
            {
                Name = "MOTHER",
                Code = "MOTHER",
                Updated = DateTime.Now,
            };
            var motherEntity = await context.AddAsync(mother);


            var sister = new BeneficiaryRelation
            {
                Name = "SISTER",
                Code = "SISTER",
                Updated = DateTime.Now,
            };
            var sisterEntity = await context.AddAsync(sister);

            var uncle = new BeneficiaryRelation
            {
                Name = "UNCLE",
                Code = "UNCLE",
                Updated = DateTime.Now,
            };
            var uncleEntity = await context.AddAsync(uncle);

            var aunty = new BeneficiaryRelation
            {
                Name = "AUNTY",
                Code = "AUNTY",
                Updated = DateTime.Now,
            };
            var auntyEntity = await context.AddAsync(aunty);

            var newphew = new BeneficiaryRelation
            {
                Name = "NEWPHEW",
                Code = "NEWPHEW",
                Updated = DateTime.Now,
            };
            var newphewEntity = await context.AddAsync(newphew);

            var niece = new BeneficiaryRelation
            {
                Name = "NIECE",
                Code = "NIECE",
                Updated = DateTime.Now,
            };
            var nieceEntity = await context.AddAsync(niece);

            var inlaw = new BeneficiaryRelation
            {
                Name = "INLAW",
                Code = "INLAW",
                Updated = DateTime.Now,
            };
            var inlawEntity = await context.AddAsync(inlaw);

            var unknown = new BeneficiaryRelation
            {
                Name = "UNKNOWN",
                Code = "UNKNOWN",
                Updated = DateTime.Now,
            };
            var unknownEntity = await context.AddAsync(unknown);

            #endregion

            #region CASE ENABLER
            var demoCaseEnabler = new CaseEnabler
            {
                Name = "DEMO",
                Code = "DEMO",
                Updated = DateTime.Now,
            };
            var demoCaseEnablerEntity = await context.CaseEnabler.AddAsync(demoCaseEnabler);

            var doubtCaseEnabler = new CaseEnabler
            {
                Name = "DOUBT-DETAILS",
                Code = "DOUBT",
                Updated = DateTime.Now,
            };
            var doubtCaseEnablerEntity = await context.CaseEnabler.AddAsync(doubtCaseEnabler);

            var highAmountCaseEnabler = new CaseEnabler
            {
                Name = "HIGH-PREMIUM",
                Code = "HIGH",
                Updated = DateTime.Now,
            };
            var highAmountCaseEnablerEntity = await context.CaseEnabler.AddAsync(highAmountCaseEnabler);

            var unknownCaseEnabler = new CaseEnabler
            {
                Name = "UNKNOWN",
                Code = "UNKNOWN",
                Updated = DateTime.Now,
            };
            var unknownCaseEnablerCaseEnablerEntity = await context.CaseEnabler.AddAsync(unknownCaseEnabler);
            #endregion

            #region COST CENTRE

            var demoCentre = new CostCentre
            {
                Name = "DEMO",
                Code = "DEMO",
                Updated = DateTime.Now,
            };

            var demoCentreEntity = await context.CostCentre.AddAsync(demoCentre);

            var loansCostCentre = new CostCentre
            {
                Name = "LOANS",
                Code = "LOANS",
                Updated = DateTime.Now,
            };

            var loansCostCentreEntity = await context.CostCentre.AddAsync(loansCostCentre);

            var financeCostCentre = new CostCentre
            {
                Name = "FINANCE",
                Code = "FINANCE",
                Updated = DateTime.Now,
            };

            var financeCostCentreEntity = await context.CostCentre.AddAsync(financeCostCentre);

            var unknownCostCentre = new CostCentre
            {
                Name = "UNKNOWN",
                Code = "UNKNOWN",
                Updated = DateTime.Now,
            };

            var unknownCostCentreEntity = await context.CostCentre.AddAsync(unknownCostCentre);
            #endregion

            #region INCOME_TYPE

            var unknownIncome = new IncomeType
            {
                Name = "UNKNOWN",
                Code = "UNKNOWN",
                Updated = DateTime.Now,
            };
            await context.IncomeType.AddAsync(unknownIncome);

            var NoIncome = new IncomeType
            {
                Name = "NO INCOME",
                Code = "None",
                Updated = DateTime.Now,
            };
            await context.IncomeType.AddAsync(NoIncome);

            var lowIncome = new IncomeType
            {
                Name = "0 - 2.5 Lac",
                Code = "LowIncome",
                Updated = DateTime.Now,
            };
            await context.IncomeType.AddAsync(lowIncome);

            var mediumIncome = new IncomeType
            {
                Name = "2.5 - 5 Lac",
                Code = "MediumIncome",
                Updated = DateTime.Now,
            };
            await context.IncomeType.AddAsync(mediumIncome);

            var highIncome = new IncomeType
            {
                Name = "5 - 10 Lac",
                Code = "HighIncome",
                Updated = DateTime.Now,
            };
            await context.IncomeType.AddAsync(highIncome);

            var veryHighIncome = new IncomeType
            {
                Name = "10 - 30 Lac",
                Code = "VeryHighIncome",
                Updated = DateTime.Now,
            };
            await context.IncomeType.AddAsync(veryHighIncome);

            var ultraHighIncome = new IncomeType
            {
                Name = "Above 30 Lac",
                Code = "UltraHighIncome",
                Updated = DateTime.Now,
            };
            await context.IncomeType.AddAsync(ultraHighIncome);
            #endregion

            #region EDUCATION_TYPE

            var unknownEducation = new EducationType
            {
                Name = "UNKNOWN",
                Code = "UNKNOWN",
                Updated = DateTime.Now,
            };
            await context.EducationType.AddAsync(unknownEducation);

            var illiterate = new EducationType
            {
                Name = "NO EDUCATION",
                Code = "NONE",
                Updated = DateTime.Now,
            };
            await context.EducationType.AddAsync(illiterate);

            var primary = new EducationType
            {
                Name = "PRIMARY SCHOOL",
                Code = "PRIMARY SCHOOL",
                Updated = DateTime.Now,
            };
            await context.EducationType.AddAsync(primary);

            var high = new EducationType
            {
                Name = "HIGH SCHOOL",
                Code = "HIGH SCHOOL",
                Updated = DateTime.Now,
            };
            await context.EducationType.AddAsync(high);

            var twevlth = new EducationType
            {
                Name = "12th CLASS",
                Code = "12thClass",
                Updated = DateTime.Now,
            };
            await context.EducationType.AddAsync(twevlth);

            var graduate = new EducationType
            {
                Name = "GRADUATE",
                Code = "GRADUATE",
                Updated = DateTime.Now,
            };
            await context.EducationType.AddAsync(graduate);

            var postGraduate = new EducationType
            {
                Name = "POST GRADUATE",
                Code = "POSTGRADUATE",
                Updated = DateTime.Now,
            };
            await context.EducationType.AddAsync(postGraduate);

            #endregion

            #region OCCUPATION_TYPE
            var unknownOccupation = new OccupationType
            {
                Name = "UNKNOWN",
                Code = "UNKNOWN",
                Updated = DateTime.Now,
            };
            await context.OccupationType.AddAsync(unknownOccupation);

            var engineer = new OccupationType
            {
                Name = "ENGINEER",
                Code = "ENGINEER",
                Updated = DateTime.Now,
            };
            await context.OccupationType.AddAsync(engineer);

            var doctor = new OccupationType
            {
                Name = "DOCTOR",
                Code = "DOCTOR",
                Updated = DateTime.Now,
            };
            await context.OccupationType.AddAsync(doctor);

            var accountant = new OccupationType
            {
                Name = "ACCOUNTANT",
                Code = "ACCOUNTANT",
                Updated = DateTime.Now,
            };
            await context.OccupationType.AddAsync(accountant);

            var professional = new OccupationType
            {
                Name = "PROFESSIONAL",
                Code = "PROFESSIONAL",
                Updated = DateTime.Now,
            };
            await context.OccupationType.AddAsync(professional);

            var selfEmployed = new OccupationType
            {
                Name = "SELF EMPLOYED",
                Code = "SELF EMPLOYED",
                Updated = DateTime.Now,
            };
            await context.OccupationType.AddAsync(selfEmployed);

            #endregion
        }
    }
}
