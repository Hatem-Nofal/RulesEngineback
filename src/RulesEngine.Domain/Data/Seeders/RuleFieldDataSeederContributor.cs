using RulesEngine.Common.Consts;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

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
                new RuleField("Diagnoses", "Diagnoses", DataTypes.List, "Diagnosis"),
                new RuleField("Activities", "Activities", DataTypes.List, "Activity"),
                new RuleField("ActivitiesQuantities", "Activities Quantities", DataTypes.Number, "Activity"),
                new RuleField("ActivityQuantity", "Activity Quantity", DataTypes.Number, "Activity"),
                new RuleField("DiagnosisCounts", "Diagnosis Counts", DataTypes.Number, "Diagnosis"),
                new RuleField("Activity", "Activity", DataTypes.List, "Activity"),
                new RuleField("NationalId", "National ID", DataTypes.Number, "Patient"),
                new RuleField("Age", "Age", DataTypes.Number, "Patient"),
                new RuleField("Gender", "Gender", DataTypes.Enum, "Patient", JsonSerializer.Serialize(new[] { "Male", "Female" })),
                new RuleField("ClaimedNet", "Claimed Net", DataTypes.Decimal, "Claim"),
                new RuleField("EnteredNet", "Entered Net", DataTypes.Decimal, "Claim"),
                new RuleField("ClaimedGross", "Claimed Gross", DataTypes.Decimal, "Claim"),
                new RuleField("EnteredGross", "Entered Gross", DataTypes.Decimal, "Claim"),
                new RuleField("ClaimStartDate", "Claim Start Date", DataTypes.DateTime, "Claim"),
                new RuleField("ClaimEndDate", "Claim End Date", DataTypes.DateTime, "Claim"),
                new RuleField("BatchType", "Batch Type", DataTypes.List, "Batch"),
                new RuleField("BatchServiceMonth", "Batch Service Month", DataTypes.Number, "Batch"),
                new RuleField("BatchServiceYear", "Batch Service Year", DataTypes.Number, "Batch"),
                new RuleField("ClaimTags", "Claim Tags", DataTypes.List, "Claim"),
                new RuleField("SpecialitiesCode", "Specialities", DataTypes.List, "Speciality"),
                new RuleField("GenericGroup", "Generic Group", DataTypes.List, "Drug"),
                new RuleField("GenericsGroups", "Generics Groups", DataTypes.List, "Drug"),
                new RuleField("DiagnosisGroupCodes", "Diagnosis Groups", DataTypes.List, "Diagnosis"),
            });
        }
    }
}
