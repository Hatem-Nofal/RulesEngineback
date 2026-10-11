using System;
using System.Collections.Generic;
using System.Text;

namespace RulesEngine.Entities.WorkFlows.Rules
{
    public class RuleValidationException(string message) : Exception(message);

}
