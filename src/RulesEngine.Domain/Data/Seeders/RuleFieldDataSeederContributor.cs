using RulesEngine.Common.Consts;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace RulesEngine.Data.Seeders
{
    public class RuleFieldDataSeederContributor : IDataSeedContributor,
    ITransientDependency
    {
        private readonly IRepository<RuleField> _ruleFieldRepository;

        public RuleFieldDataSeederContributor(IRepository<RuleField> ruleFieldRepository)
        {
            _ruleFieldRepository = ruleFieldRepository;
        }

        [UnitOfWork]
        public async Task SeedAsync(DataSeedContext context)
        {

            if (await _ruleFieldRepository.GetCountAsync() > 0)
            {
                return;
            }

            await _ruleFieldRepository.InsertManyAsync(new[]
            {
                new RuleField("Diagnoses", "Diagnoses", DataTypes.List),
                new RuleField("Activities", "Activities", DataTypes.List),
                new RuleField("ActivitiesQuantities", "Activities Quantities", DataTypes.Number),
                new RuleField("ActivityQuantity", "Activity Quantity", DataTypes.Number),
                new RuleField("DiagnosisCounts", "Diagnosis Counts", DataTypes.Number),
                new RuleField("Activity", "Activity", DataTypes.List),
                new RuleField("NationalId", "National ID", DataTypes.Number),
                new RuleField("Age", "Age", DataTypes.Number),
                new RuleField("Gender", "Gender", DataTypes.Enum, JsonSerializer.Serialize(new[] { "Male", "Female" })),
                new RuleField("ClaimedNet", "Claimed Net", DataTypes.Decimal),
                new RuleField("EnteredNet", "Entered Net", DataTypes.Decimal),
                new RuleField("ClaimedGross", "Claimed Gross", DataTypes.Decimal),
                new RuleField("EnteredGross", "Entered Gross", DataTypes.Decimal),
                new RuleField("ClaimStartDate", "Claim Start Date", DataTypes.DateTime),
                new RuleField("ClaimEndDate", "Claim End Date", DataTypes.DateTime),
                new RuleField("BatchType", "Batch Type", DataTypes.Enum, JsonSerializer.Serialize(new[] { "outpatient", "inpatient", "ER","prostheticDevices","CommercialPharmacy"})),
                new RuleField("BatchServiceMonth", "Batch Service Month", DataTypes.Number),
                new RuleField("BatchServiceYear", "Batch Service Year", DataTypes.Number),
                new RuleField("ClaimTags", "Claim Tags", DataTypes.List),
                new RuleField("Claimspeccode", "Claimspeccode", DataTypes.List),
                new RuleField("PrimaryGenericsGroup", "Generic Group", DataTypes.List),
                new RuleField("GenericsGroups", "Generics Groups", DataTypes.List),
                new RuleField("DiagnosisGroupCodes", "Diagnosis Groups", DataTypes.List),
            });
        }
    }
}
