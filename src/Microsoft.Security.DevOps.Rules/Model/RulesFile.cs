// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules.Model
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class RulesFile : RuleCollection
    {
        [DataMember(Name = "$schema", EmitDefaultValue = false, Order = 0)]
        public string? Schema { get; set; }

        [DataMember(Name = "analyzers", EmitDefaultValue = false, Order = 20)]
        public List<RuleCollection?>? Analyzers { get; set; }

        [DataMember(Name = "rulesets", EmitDefaultValue = false, Order = 10)]
        public List<RuleCollection?>? Rulesets { get; set; }
    }
}
