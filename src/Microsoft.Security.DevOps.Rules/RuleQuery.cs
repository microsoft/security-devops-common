// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class RuleQuery
    {
        [DataMember(Name = "ruleId", Order = 10)]
        public string? RuleId { get; set; }

        [DataMember(Name = "rulesetName", Order = 20)]
        public string? RulesetName { get; set; }

        [DataMember(Name = "analyzerName", Order = 30)]
        public string? AnalyzerName { get; set; }

        internal bool IsValid { get; set; }

        [DataMember(Name = "all", Order = 100)]
        public bool All { get; set; }

        internal QueryType Type { get; set; } = QueryType.Undefined;
    }
}
