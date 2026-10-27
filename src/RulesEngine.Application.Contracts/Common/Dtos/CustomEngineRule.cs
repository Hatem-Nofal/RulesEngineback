using RulesEngine.Common.Enums;
using EngineRule = RulesEngine.Models.Rule;

namespace RulesEngine.Common.Dtos
{
    public class CustomEngineRule : EngineRule
    {
        public ViolationType ViolationType { get; set; }
    }
}
