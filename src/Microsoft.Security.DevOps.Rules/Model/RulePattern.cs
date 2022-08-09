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
    public class RulePattern : PropertyBagObject
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

        [DataMember(Name = "rule", EmitDefaultValue = false, Order = 10)]
        public Rule Rule { get; set; }
    }
}
