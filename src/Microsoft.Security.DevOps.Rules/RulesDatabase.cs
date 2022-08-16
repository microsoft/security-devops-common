// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using Microsoft.Security.DevOps.Rules.Model;
    using Newtonsoft.Json;
    using System.Diagnostics.CodeAnalysis;

    public class RulesDatabase : IRulesDatabase
    {
        private RulesFile? rulesFile;
        public virtual RulesFile RulesFile
        {
            get
            {
                if (rulesFile is null)
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
        public virtual void Load()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "microsoft.json");
            Load(filePath);
        }

        public virtual void Load(string filePath)
        {
            rulesFile = Read<RulesFile?>(filePath);
        }

        internal virtual T? Read<T>(string filePath)
        {
            string contents = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<T>(contents);
        }

        public virtual QueryResult Query(RuleQuery? query)
        {
            Validate(query);

            var result = new QueryResult(query);

            QueryAnalyzers(query, result);

            if (query.Type == QueryType.FindAnalyzer)
            {
                return result;
            }

            if (query.Type == QueryType.FindRule && result.Rule != null)
            {
                return result;
            }

            QueryRuleset(query, result);

            if (query.Type == QueryType.FindRuleset)
            {
                return result;
            }

            if (query.Type == QueryType.FindRule && result.Rule != null)
            {
                return result;
            }

            QueryRules(query, RulesFile, result);

            return result;
        }

        internal virtual QueryResult QueryAnalyzers(RuleQuery query, QueryResult? result = null)
        {
            result ??= new QueryResult(query);

            result.Analyzer = GetAnalyzer(query);
            result.Rule = FindRuleById(query?.RuleId, result.Analyzer?.Rules, true, true);

            return result;
        }

        internal virtual QueryResult QueryRuleset(RuleQuery query, QueryResult? result = null)
        {
            result ??= new QueryResult(query);

            result.Ruleset = GetRuleset(query);
            result.Rule = FindRuleById(query?.RuleId, result.Ruleset?.Rules, true, true);

            return result;
        }

        internal virtual QueryResult QueryRules(RuleQuery query, RuleCollection? ruleCollection, QueryResult? result = null)
        {
            result ??= new QueryResult(query);

            if (ruleCollection is null)
            {
                return result;
            }

            result.Rule = FindRuleById(query?.RuleId, ruleCollection?.Rules);

            if (query?.Type == QueryType.FindRule && result.Rule != null)
            {
                return result;
            }

            QueryRulePattern(query, ruleCollection?.RulePatterns);

            return result;
        }

        internal virtual QueryResult QueryRulePattern(RuleQuery query, List<RulePattern?>? rulePatterns, QueryResult? result = null)
        {
            result ??= new QueryResult(query);

            if (query.Type != QueryType.FindRule
                || query.Type != QueryType.FindRule
                || rulePatterns?.Any() != true)
            {
                return result;
            }

            foreach (RulePattern? rulePattern in rulePatterns)
            {
                if (IsMatch(query.RuleId, rulePattern))
                {
                    result.RulePattern = rulePattern;
                    result.Rule = rulePattern.Rule;

                    if (result.Rule == null)
                    {
                        continue;
                    }

                    result.Rule.Pattern = rulePattern;
                    break;
                }
            }

            return result;
        }

        public virtual Rule? GetRule(RuleQuery? query)
        {
            QueryResult result = Query(query);
            return result?.Rule;
        }

        public virtual RuleCollection? GetAnalyzer(RuleQuery? query)
        {
            return FindRuleCollectionByName(query?.AnalyzerName, RulesFile.Analyzers, true);
        }

        public virtual RuleCollection? GetRuleset(RuleQuery? query)
        {
            return FindRuleCollectionByName(query?.RulesetName, RulesFile.Rulesets, true);
        }

        public virtual RuleCategory GetCategoryEnum(RuleQuery? query)
        {
            QueryResult result = Query(query);
            return result?.Category ?? RuleCategory.Undefined;
        }

        public virtual string? GetCategoryString(RuleQuery? query)
        {
            QueryResult result = Query(query);
            return result?.CategoryString;
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

            if (rule is null && (searchAlternativeIds || searchDeprecatedIds))
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

        internal virtual bool IsMatch(string? ruleId, [NotNullWhen(true)] RulePattern? rulePattern)
        {
            return !string.IsNullOrWhiteSpace(ruleId) && (rulePattern?.Regex?.IsMatch(ruleId) ?? false);
        }

        public virtual RuleCollection? FindRuleCollectionByName(
            string? name,
            List<RuleCollection?>? ruleCollections)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return ruleCollections?.Find(rulesInfo => string.Equals(rulesInfo?.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public virtual RuleCollection? FindRuleCollectionByName(
            string? name,
            List<RuleCollection?>? ruleCollections,
            bool searchAlternativeNames)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            RuleCollection? ruleCollection = FindRuleCollectionByName(name, ruleCollections);

            if (ruleCollection is null & searchAlternativeNames)
            {
                ruleCollection = ruleCollections?.Find(ruleCollection => ruleCollection?.AlternativeNames?.Contains(name, StringComparer.OrdinalIgnoreCase) == true);
            }

            return ruleCollection;
        }

        internal virtual void Validate([NotNull] RuleQuery? query)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (!string.IsNullOrWhiteSpace(query.RuleId))
            {
                query.Type = QueryType.FindRule;
            }

            if (!string.IsNullOrWhiteSpace(query.AnalyzerName))
            {
                query.Type = QueryType.FindAnalyzer;
            }

            if (!string.IsNullOrWhiteSpace(query.RulesetName))
            {
                query.Type = QueryType.FindRuleset;
            }

            if (query.Type == QueryType.Undefined)
            {
                throw new RuleQueryInsufficientArgumentsException(query);
            }

            if (query.All)
            {
                query.Type = QueryType.All;
            }
        }
    }
}
