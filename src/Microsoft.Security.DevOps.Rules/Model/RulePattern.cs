// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules.Model
{
    using System;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;

    [DataContract]
    public class RulePattern : PropertyBag
    {
        private string? typeString;

        [DataMember(Name = "type", EmitDefaultValue = false, Order = 10)]
        public string? TypeString
        {
            get
            {
                return typeString;
            }
            set
            {
                typeString = value;
                if (!string.IsNullOrWhiteSpace(value)
                   && !Enum.TryParse(value, true, out patternType))
                {
                    patternType = RulePatternType.Unknown;
                }
            }
        }

        private RulePatternType patternType = RulePatternType.Undefined;
        public RulePatternType Type
        {
            get
            {
                return patternType;
            }
            set
            {
                patternType = value;
                typeString = value.ToString();
            }
        }

        [DataMember(Name = "pattern", EmitDefaultValue = false, Order = 20)]
        public string? Pattern { get; set; }

        [DataMember(Name = "ignoreCase", EmitDefaultValue = false, Order = 21)]
        public bool? IgnoreCase { get; set; }

        private Regex? regex = null;
        public Regex? Regex
        {
            get
            {
                if (regex == null && !string.IsNullOrWhiteSpace(Pattern))
                {
                    regex = new Regex(Pattern, IgnoreCase == true ? RegexOptions.IgnoreCase : RegexOptions.None);
                }

                return regex;
            }
        }

        [DataMember(Name = "rule", EmitDefaultValue = false, Order = 30)]
        public Rule? Rule { get; set; }
    }
}
