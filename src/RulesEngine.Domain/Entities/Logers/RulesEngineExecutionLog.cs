using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Volo.Abp.Domain.Entities;

namespace RulesEngine.Entities.Logers
{
    [Table("RulesEngineExecutionLogs", Schema = "Logers")]

    public sealed class RulesEngineExecutionLog : Entity<int>
    {


        public string WorkFlowName { get; private set; }
        public string? Rule { get; private set; }
        public string? ErrorType { get; private set; }
        public string? ErrorMessage { get; private set; }
        public string? ClaimNumber { get; private set; }
        public string Expression { get; private set; }
        public string Activity { get; private set; }
        public DateTime CreationTime { get; private set; } = DateTime.Now;
        public bool IsViolated { get; private set; } = false;
        public static RulesEngineExecutionLog Create(string workFlowName, string? rule, string? errorType, string? errorMessage, string? claimNumber, string expression, string activity, bool isViolated)
        {
            return new RulesEngineExecutionLog()
            {


                WorkFlowName = workFlowName,
                Rule = rule,
                ErrorType = errorType,
                ErrorMessage = errorMessage,
                ClaimNumber = claimNumber,
                Expression = expression,
                Activity = activity,
                CreationTime = DateTime.Now,
                IsViolated= isViolated
            };
        }

    }
}
