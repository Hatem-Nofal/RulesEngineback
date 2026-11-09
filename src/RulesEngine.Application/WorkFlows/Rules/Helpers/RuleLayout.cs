using RulesEngine.Common.Enums;
using RulesEngine.Entities.WorkFlows.Rules.RuleBuilders;
using RulesEngine.WorkFlows.Dtos.RuleBuildersDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RulesEngine.WorkFlows.Rules.Helpers
{
    /// <summary>One filled-in condition coming from an import row: which layout slot it targets, and the node itself.</summary>
    public sealed record LayoutCondition(string FieldKey, int Occurrence, RuleNodeDto Node);

    /// <summary>
    /// The layout of a workflow's rule form, loaded from <c>workflow.Nodes</c>: groups, AND/OR joins and the ordered
    /// condition "slots". A slot is identified by (field key, occurrence) where occurrence is the 1-based position of
    /// that field key among the layout's conditions in depth-first order - the same rule the sample sheet uses to name
    /// duplicate columns ("Age (Primary)", "Age (Secondary)").
    /// Bulk import fills the slots from a row instead of building a flat list, so groups survive.
    /// </summary>
    public sealed class RuleLayout
    {
        private readonly RuleNodeDto _root;
        private readonly Dictionary<string, RuleNodeDto> _slots;

        private RuleLayout(RuleNodeDto root, Dictionary<string, RuleNodeDto> slots)
        {
            _root = root;
            _slots = slots;
        }

        private static string SlotId(string key, int occurrence) => $"{key.Trim().ToLowerInvariant()}#{occurrence}";

        /// <summary>Returns null when the workflow has no saved layout (callers fall back to a flat AND list).</summary>
        public static RuleLayout? FromNodes(IEnumerable<RuleNode>? nodes)
        {
            var list = nodes?.ToList();
            if (list == null || list.Count == 0) return null;

            var byParent = list.Where(n => n.ParentId.HasValue).ToLookup(n => n.ParentId!.Value);
            var rootNode = list.Where(n => n.ParentId == null).OrderBy(n => n.Order).FirstOrDefault();
            if (rootNode == null || rootNode.NodeType != RuleNodeType.Group) return null;

            var slots = new Dictionary<string, RuleNodeDto>();
            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            RuleNodeDto Build(RuleNode n, LogicalOperator? parentOp, bool isFirst)
            {
                var join = isFirst ? (LogicalOperator?)null : n.LogicalJoinOperator ?? parentOp ?? LogicalOperator.And;

                if (n.NodeType == RuleNodeType.Condition)
                {
                    var key = n.FieldKey ?? string.Empty;
                    seen[key] = seen.GetValueOrDefault(key) + 1;
                    var leaf = new RuleNodeDto
                    {
                        NodeType = RuleNodeType.Condition,
                        FieldKey = n.FieldKey,
                        Operator = n.Operator,
                        LogicalJoinOperator = join
                    };
                    slots[SlotId(key, seen[key])] = leaf;
                    return leaf;
                }

                var groupOp = n.LogicalOperator ?? LogicalOperator.And;
                var group = new RuleNodeDto
                {
                    NodeType = RuleNodeType.Group,
                    LogicalOperator = groupOp,
                    LogicalJoinOperator = join
                };

                var children = byParent[n.Id].OrderBy(c => c.Order).ToList();
                for (var i = 0; i < children.Count; i++)
                    group.Children.Add(Build(children[i], groupOp, i == 0));

                return group;
            }

            var root = Build(rootNode, null, true);
            return slots.Count == 0 ? null : new RuleLayout(root, slots);
        }

        /// <summary>The operator the layout preselects for a slot (used when the sheet's operator cell is blank).</summary>
        public RuleOperator? DefaultOperatorFor(string fieldKey, int occurrence) =>
            _slots.TryGetValue(SlotId(fieldKey, occurrence), out var slot) ? slot.Operator : null;

        /// <summary>
        /// Builds a fresh rule tree with the layout's shape: every slot that has a filled condition keeps its place and
        /// join operator; empty slots are dropped, empty groups disappear, and a group left with a single item collapses
        /// into that item. Conditions that target no slot are appended to the root joined with AND (nothing is silently lost).
        /// </summary>
        public RuleNodeDto Bind(IReadOnlyList<LayoutCondition> conditions)
        {
            var pending = new Dictionary<string, LayoutCondition>();
            var extras = new List<LayoutCondition>();
            foreach (var c in conditions)
            {
                if (!pending.TryAdd(SlotId(c.FieldKey, c.Occurrence), c))
                    extras.Add(c);
            }

            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var root = Instantiate(_root, pending, seen, isRoot: true)
                       ?? new RuleNodeDto { NodeType = RuleNodeType.Group, LogicalOperator = _root.LogicalOperator ?? LogicalOperator.And };

            // Only one group survived (the others were blank): hoist its items so we don't emit a redundant "((...))".
            while (root.Children.Count == 1 && root.Children[0].NodeType == RuleNodeType.Group)
            {
                var inner = root.Children[0];
                root.Children.Clear();
                root.Children.AddRange(inner.Children);
            }

            // Slots the layout does not know about + duplicates: keep the user's input instead of dropping it.
            extras.AddRange(pending.Values);
            foreach (var extra in extras.OrderBy(x => x.Occurrence))
            {
                extra.Node.LogicalJoinOperator = LogicalOperator.And;
                root.Children.Add(extra.Node);
            }

            NormalizeJoins(root);
            return root;
        }

        /// <summary>Same as <see cref="Bind"/> for workflows without a layout: one flat group, everything AND-ed.</summary>
        public static RuleNodeDto BuildFlat(IReadOnlyList<LayoutCondition> conditions)
        {
            var root = new RuleNodeDto { NodeType = RuleNodeType.Group, LogicalOperator = LogicalOperator.And };
            foreach (var c in conditions)
                root.Children.Add(c.Node);
            NormalizeJoins(root);
            return root;
        }

        private static RuleNodeDto? Instantiate(
            RuleNodeDto template,
            Dictionary<string, LayoutCondition> pending,
            Dictionary<string, int> seen,
            bool isRoot)
        {
            if (template.NodeType == RuleNodeType.Condition)
            {
                var key = template.FieldKey ?? string.Empty;
                seen[key] = seen.GetValueOrDefault(key) + 1;   // always count, so occurrences stay aligned with the sheet

                if (!pending.Remove(SlotId(key, seen[key]), out var condition))
                    return null;

                condition.Node.LogicalJoinOperator = template.LogicalJoinOperator;
                return condition.Node;
            }

            var group = new RuleNodeDto
            {
                NodeType = RuleNodeType.Group,
                LogicalOperator = template.LogicalOperator,
                LogicalJoinOperator = template.LogicalJoinOperator
            };

            foreach (var child in template.Children)
            {
                var built = Instantiate(child, pending, seen, isRoot: false);
                if (built != null) group.Children.Add(built);
            }

            if (group.Children.Count == 0) return isRoot ? group : null;

            if (!isRoot && group.Children.Count == 1)
            {
                var only = group.Children[0];
                only.LogicalJoinOperator = group.LogicalJoinOperator;
                return only;
            }

            return group;
        }

        /// <summary>First child has no join; every later child must have one (the validator requires it).</summary>
        private static void NormalizeJoins(RuleNodeDto group)
        {
            for (var i = 0; i < group.Children.Count; i++)
            {
                var child = group.Children[i];
                child.LogicalJoinOperator = i == 0
                    ? null
                    : child.LogicalJoinOperator ?? group.LogicalOperator ?? LogicalOperator.And;

                if (child.NodeType == RuleNodeType.Group)
                    NormalizeJoins(child);
            }
        }

        /// <summary>Readable one-line description, e.g. <c>(Activity AND Qyan) AND (Age [GreaterThanOrEqual] AND Age 2 [LessThanOrEqual])</c>.</summary>
        public string Describe(Func<string, int, string> columnLabel)
        {
            var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            string Walk(RuleNodeDto n, bool isRoot)
            {
                if (n.NodeType == RuleNodeType.Condition)
                {
                    var key = n.FieldKey ?? string.Empty;
                    seen[key] = seen.GetValueOrDefault(key) + 1;
                    var label = columnLabel(key, seen[key]);
                    return n.Operator.HasValue ? $"{label} [{n.Operator.Value}]" : label;
                }

                var sb = new StringBuilder();
                for (var i = 0; i < n.Children.Count; i++)
                {
                    if (i > 0)
                        sb.Append(n.Children[i].LogicalJoinOperator == LogicalOperator.Or ? " OR " : " AND ");
                    sb.Append(Walk(n.Children[i], false));
                }

                return isRoot || n.Children.Count <= 1 ? sb.ToString() : $"({sb})";
            }

            return Walk(_root, true);
        }
    }
}