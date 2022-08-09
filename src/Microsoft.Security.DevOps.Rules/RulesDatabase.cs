// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
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
            private set
            {
                rulesFile = value;
            }
        }

        public RulesDatabase()
        {

        }

        [MemberNotNull(nameof(RulesFile))]
        [MemberNotNull(nameof(rulesFile))]
        public void Load()
        {
            RulesFile = new RulesFile();
        }

        public void Load(string filePath)
        {
            RulesFile = new RulesFile();
        }

        public virtual IRuleCategoryInfo GetCategoryInfo(RuleQuery? query)
        {
            IRuleCategoryInfo rule = GetRule(query);
            return rule;
        }

        public virtual RuleCategory GetCategoryEnum(RuleQuery? query)
        {
            IRuleCategoryInfo? info = GetCategoryInfo(query);
            return info?.Category ?? RuleCategory.Undefined;
        }

        public virtual string? GetCategoryString(RuleQuery? query)
        {
            IRuleCategoryInfo? info = GetCategoryInfo(query);
            return info?.CategoryString;
        }

        public virtual Rule GetRule(RuleQuery? query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            if (string.IsNullOrWhiteSpace(query.RuleId) && string.IsNullOrWhiteSpace(query.RulePattern))
            {
                throw new RuleQueryInsufficientArgumentsException(query);
            }

            RulesInfo? analyzerInfo = GetAnalyzerInfo(query);

            // Analyzer
                // Check alt name
            // Rulesets
                // Check alt name
            // Rules
                // check alt id
            // RulePatterns
                // 

            return null;
        }

        public virtual Rule GetRule(string ruleId, RulesInfo rules)
        {
            return null;
        }

        public virtual RulesInfo? GetAnalyzerInfo(RuleQuery? query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return Get<RulesInfo?>(query.AnalyzerName, RulesFile.Analyzers, RulesFile.AlternativeAnalyzerNames);
        }

        internal virtual T? Get<T>(
            string? key,
            Dictionary<string, T?>? dict,
            Dictionary<string, string?>? altKeyDict)
            where T : RulesInfo?
        {
            T? value = null;

            if (!string.IsNullOrWhiteSpace(key)
                && (dict?.TryGetValue(key, out value) != true || value == null)
                && altKeyDict?.TryGetValue(key, out string? realAnalyzerName) == true
                && !string.IsNullOrWhiteSpace(realAnalyzerName))
            {
                dict?.TryGetValue(realAnalyzerName, out value);
            }

            return value;
        }
    }
}
