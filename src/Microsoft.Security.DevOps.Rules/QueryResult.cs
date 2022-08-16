// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using Microsoft.CodeAnalysis.Sarif;
    using Microsoft.Security.DevOps.Rules.Model;
    using System;
    using System.Linq;

    public class QueryResult : RulesFile
    {
        public QueryResult(RuleQuery? query)
        {
            Query = query;
        }

        public RuleQuery? Query { get; internal set; }

        private bool? success = null;
        public bool Success
        {
            get
            {
                if (success != null)
                {
                    return success.Value;
                }

                if (Query == null)
                {
                    return false;
                }

                success = Rule != null && (Query.Type == QueryType.FindRule || Query.Type == QueryType.All);
                success = Analyzer != null && (Query.Type == QueryType.FindAnalyzer || Query.Type == QueryType.All);
                success = Ruleset != null && (Query.Type == QueryType.FindRuleset || Query.Type == QueryType.All);

                return success.Value;
            }
            internal set
            {
                success = value;
            }
        }

        public override string? CategoryString
        {
            get
            {
                if (Rule is not null && Rule.Category != RuleCategory.Undefined)
                {
                    base.CategoryString = Rule.CategoryString;
                }
                else if (Analyzer is not null && Analyzer.Category != RuleCategory.Undefined)
                {
                    base.CategoryString = Analyzer.CategoryString;
                }
                else if (Ruleset is not null && Ruleset.Category != RuleCategory.Undefined)
                {
                    base.CategoryString = Ruleset.CategoryString;
                }

                return base.CategoryString;
            }
            set
            {
                base.CategoryString = value;
            }
        }

        public override RuleCategory Category
        {
            get
            {
                if (base.CategoryString is null)
                {
                    categoryString = CategoryString;
                }
                return base.Category;
            }
            set
            {
                base.Category = value;
            }
        }

        public RuleCollection? Analyzer
        {
            get
            {
                return Analyzers?.First();
            }
            internal set
            {
                if (value != null)
                {
                    Analyzers ??= new List<RuleCollection?>();
                    Analyzers.Add(value);
                }
            }
        }
        public RuleCollection? Ruleset
        {
            get
            {
                return Rulesets?.First();
            }
            internal set
            {
                if (value != null)
                {
                    Rulesets ??= new List<RuleCollection?>();
                    Rulesets.Add(value);
                }
            }
        }

        public Rule? Rule
        {
            get
            {
                return Rules?.First();
            }
            internal set
            {
                if (value != null)
                {
                    Rules ??= new List<Rule?>();
                    Rules.Add(value);
                }
            }
        }
        public RulePattern? RulePattern
        {
            get
            {
                return RulePatterns?.First();
            }
            internal set
            {
                if (value != null)
                {
                    RulePatterns ??= new List<RulePattern?>();
                    RulePatterns.Add(value);
                }
            }
        }
    }
}
