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

        [DataMember(Name = "rulePattern", Order = 11)]
        public string? RulePattern { get; set; }

        [DataMember(Name = "rulesetName", Order = 20)]
        public string? RulesetName { get; set; }

        [DataMember(Name = "rulesetVersion", Order = 40)]
        public string? RulesetVersion { get; set; }

        [DataMember(Name = "analyzerName", Order = 30)]
        public string? AnalyzerName { get; set; }

        [DataMember(Name = "analyzerVersion", Order = 40)]
        public string? AnalyzerVersion { get; set; }
    }
}
