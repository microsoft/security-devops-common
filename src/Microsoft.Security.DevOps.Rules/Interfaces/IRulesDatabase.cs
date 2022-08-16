// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules.Interfaces
{
    using Microsoft.Security.DevOps.Rules.Model;

    public interface IRulesDatabase
    {
        void Load();
        void Load(string filePath);

        IRuleCategoryInfo? GetCategoryInfo(RuleQuery? query);
        RuleCategory? GetCategoryEnum(RuleQuery? query);
        string? GetCategoryString(RuleQuery? query);

        Rule? GetRule(RuleQuery? query);
        Rule? GetRule(RuleQuery? query, RuleCollection? ruleCollection);

        RuleCollection? GetAnalyzer(RuleQuery? query);
        Rule? GetAnalyzerRule(RuleQuery? query);

        RuleCollection? GetRuleset(RuleQuery? query);
        Rule? GetRulesetRule(RuleQuery? query);

        Rule? FindRuleById(string? ruleId, List<Rule?>? rules);
        Rule? FindRuleById(
            string? ruleId,
            List<Rule?>? rules,
            bool searchAlternativeIds = false,
            bool searchDeprecatedIds = false);
        Rule? FindRuleByPattern(string? ruleId, List<RulePattern?>? rulePatterns);

        RuleCollection? FindRuleCollectionByName(List<RuleCollection?>? ruleCollections, string? name);
        RuleCollection? FindRuleCollectionByName(
            List<RuleCollection?>? ruleCollections,
            string? name,
            bool searchAlternativeNames);
    }
}
