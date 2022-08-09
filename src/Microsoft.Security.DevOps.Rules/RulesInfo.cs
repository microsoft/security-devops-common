// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class RulesInfo : IRuleCategoryInfo
    {
        [DataMember(Name = "name", EmitDefaultValue = false, Order = 10)]
        public string? Name { get; set; }

        [DataMember(Name = "version", EmitDefaultValue = false, Order = 20)]
        public string? Version { get; set; }

        private Dictionary<string, Rule?>? rules;

        [DataMember(Name = "rules", EmitDefaultValue = false, Order = 100)]
        public Dictionary<string, Rule?>? Rules
        {
            get => rules;
            set => rules = CaseInsensitiveDictionaryResolver.Instance.Resolve(value);
        }

        private Dictionary<string, string?>? alternativeRuleIds;

        [DataMember(Name = "alternativeRuleIds", EmitDefaultValue = false, Order = 101)]
        public Dictionary<string, string?>? AlternativeRuleIds
        {
            get => alternativeRuleIds;
            set => alternativeRuleIds = CaseInsensitiveDictionaryResolver.Instance.Resolve(value);
        }

        private Dictionary<string, Rule?>? rulePatterns;

        [DataMember(Name = "rulePatterns", EmitDefaultValue = false, Order = 110)]
        public Dictionary<string, Rule?>? RulePatterns
        {
            get => rulePatterns;
            set => rulePatterns = CaseInsensitiveDictionaryResolver.Instance.Resolve(value);
        }

        private string? categoryString;

        [DataMember(Name = "category", Order = 150)]
        public string? CategoryString
        {
            get
            {
                return categoryString;
            }
            set
            {
                categoryString = value;
                Category = RuleCategoryParser.Instance.Parse(value);
            }
        }

        public RuleCategory Category { get; set; }
    }
}
