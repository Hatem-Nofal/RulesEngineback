using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RulesEngine.WorkFlows.Rules.Helpers
{
    public static class RuleNodeExtensions
    {
        public static bool ContainsId(this RuleNodeDto node, Guid id)
        {
            if (node == null)
                return false;

            if (node.Id == id)
                return true;

            return node.Children.Any(child => child.ContainsId(id)) ;
        }

        public static RuleNodeDto? FindById(this RuleNodeDto node, Guid id)
        {
            if (node == null)
                return null;

            if (node.Id == id)
                return node;

            foreach (var child in node.Children ?? [])
            {
                var result = child.FindById(id);

                if (result != null)
                    return result;
            }

            return null;
        }
    }
}
