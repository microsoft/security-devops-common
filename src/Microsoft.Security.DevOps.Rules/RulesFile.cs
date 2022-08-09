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
    public class RulesFile : RulesInfo
    {
        [DataMember(Name = "$schema", EmitDefaultValue = false, Order = 0)]
        public string? Schema { get; set; }

        private Dictionary<string, RulesInfo?>? rulesets;

        [DataMember(Name = "rulesets", EmitDefaultValue = false, Order = 10)]
        public Dictionary<string, RulesInfo?>? Rulesets
        {
            get => rulesets;
            set => rulesets = CaseInsensitiveDictionaryResolver.Instance.Resolve(value);
        }

        private Dictionary<string, string?>? alternativeRulesetNames;

        [DataMember(Name = "alternativeRulesetNames", EmitDefaultValue = false, Order = 11)]
        public Dictionary<string, string?>? AlternativeRulesetNames
        {
            get => alternativeRulesetNames;
            set => alternativeRulesetNames = CaseInsensitiveDictionaryResolver.Instance.Resolve(value);
        }

        private Dictionary<string, RulesInfo?>? analyzers;

        [DataMember(Name = "analyzers", EmitDefaultValue = false, Order = 20)]
        public Dictionary<string, RulesInfo?>? Analyzers
        {
            get => analyzers;
            set => analyzers = CaseInsensitiveDictionaryResolver.Instance.Resolve(value);
        }

        private Dictionary<string, string?>? alternativeAnalyzerNames;

        [DataMember(Name = "alternativeAnalyzerNames", EmitDefaultValue = false, Order = 21)]
        public Dictionary<string, string?>? AlternativeAnalyzerNames
        {
            get => alternativeAnalyzerNames;
            set => alternativeAnalyzerNames = CaseInsensitiveDictionaryResolver.Instance.Resolve(value);
        }
    }
}
