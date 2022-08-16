// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using Microsoft.Security.DevOps.Rules.Interfaces;
    using Microsoft.Security.DevOps.Rules.Model;
    using Newtonsoft.Json;
    using System.Diagnostics.CodeAnalysis;

    public class RulesDatabase : IRulesDatabase
    {
        private RulesFile? rulesFile;
        public RulesFile RulesFile
        {
            get
            {
                if (rulesFile == null)
                {
                    Load();
                }

                return rulesFile;
            }
        }

        public RulesDatabase()
        {

        }

        [MemberNotNull(nameof(rulesFile))]
        public void Load()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rules.json");
            Load(filePath);
        }

        public void Load(string filePath)
        {
            rulesFile = Read<RulesFile>(filePath);
        }

        internal virtual T? Read<T>(string filePath)
        {
            string contents = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(contents);
        }

        public virtual IRuleCategoryInfo? GetCategoryInfo(RuleQuery? query)
        {
            IRuleCategoryInfo? rule = GetRule(query);
            return rule;
        }

        public virtual RuleCategory? GetCategoryEnum(RuleQuery? query)
        {
            IRuleCategoryInfo? info = GetCategoryInfo(query);
            return info?.Category ?? RuleCategory.Undefined;
        }

        public virtual string? GetCategoryString(RuleQuery? query)
        {
            IRuleCategoryInfo? info = GetCategoryInfo(query);
            return info?.CategoryString;
        }

        public virtual Rule? GetRule(RuleQuery? query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (string.IsNullOrWhiteSpace(query.RuleId) && string.IsNullOrWhiteSpace(query.RulePattern))
            {
                throw new RuleQueryInsufficientArgumentsException(query);
            }

            Rule? rule = null;

            rule = GetAnalyzerRule(query);

            if (rule != null)
            {
                return rule;
            }

            rule = GetRulesetRule(query);

            if (rule != null)
            {
                return rule;
            }

            rule = GetRule(query, RulesFile);

            return rule;
        }

        public virtual Rule? GetRule(RuleQuery? query, RuleCollection? ruleCollection)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            Rule? rule = FindRuleById(query?.RuleId, ruleCollection?.Rules);

            if (rule != null)
            {
                return rule;
            }

            return FindRuleByPattern(query?.RuleId, ruleCollection?.RulePatterns);
        }

        public virtual RuleCollection? GetAnalyzer(RuleQuery? query)
        {
            return FindRuleCollectionByName(RulesFile.Analyzers, query?.AnalyzerName);
        }

        public virtual Rule? GetAnalyzerRule(RuleQuery? query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            RuleCollection? analyzer = GetAnalyzer(query);
            Rule? rule = FindRuleById(query?.RuleId, analyzer?.Rules);

            return rule;
        }

        public virtual RuleCollection? GetRuleset(RuleQuery? query)
        {
            return FindRuleCollectionByName(RulesFile.Rulesets, query?.RulesetName);
        }

        public virtual Rule? GetRulesetRule(RuleQuery? query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            RuleCollection? ruleset = GetRuleset(query);
            Rule? rule = FindRuleById(query?.RuleId, ruleset?.Rules);

            return rule;
        }

        public virtual Rule? FindRuleById(string? ruleId, List<Rule?>? rules)
        {
            return rules?.Find(rule => string.Equals(rule?.Id, ruleId, StringComparison.OrdinalIgnoreCase));
        }

        public virtual Rule? FindRuleById(
            string? ruleId,
            List<Rule?>? rules,
            bool searchAlternativeIds = false,
            bool searchDeprecatedIds = false)
        {
            Rule? rule = FindRuleById(ruleId, rules);

            if (rule == null && (searchAlternativeIds || searchDeprecatedIds))
            {
                rule = rules?.Find(rule =>
                    searchAlternativeIds && rule?.AlternativeIds?.Contains(ruleId, StringComparer.OrdinalIgnoreCase) == true
                    || searchDeprecatedIds && rule?.DeprecatedIds?.Contains(ruleId, StringComparer.OrdinalIgnoreCase) == true);
            }

            return rule;
        }

        public virtual Rule? FindRuleByPattern(string? ruleId, List<RulePattern?>? rulePatterns)
        {
            Rule? rule = null;

            if (rulePatterns?.Any() != true || string.IsNullOrWhiteSpace(ruleId))
            {
                return rule;
            }

            foreach (RulePattern? rulePattern in rulePatterns)
            {
                if (IsMatch(ruleId, rulePattern))
                {
                    rule = rulePattern?.Rule;
                    break;
                }
            }

            return rule;
        }

        internal virtual bool IsMatch(string? ruleId, RulePattern? rulePattern)
        {
            return !string.IsNullOrWhiteSpace(ruleId) && (rulePattern?.Regex?.IsMatch(ruleId) ?? false);
        }

        public virtual RuleCollection? FindRuleCollectionByName(List<RuleCollection?>? ruleCollections, string? name)
        {
            return ruleCollections?.Find(rulesInfo => string.Equals(rulesInfo?.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public virtual RuleCollection? FindRuleCollectionByName(
            List<RuleCollection?>? ruleCollections,
            string? name,
            bool searchAlternativeNames)
        {
            RuleCollection? ruleCollection = FindRuleCollectionByName(ruleCollections, name);

            if (name == null & searchAlternativeNames)
            {
                ruleCollection = ruleCollections?.Find(ruleCollection => ruleCollection?.AlternativeNames?.Contains(name, StringComparer.OrdinalIgnoreCase) == true);
            }

            return ruleCollection;
        }
    }
}
