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
    public class RuleCollection : PropertyBag, IRuleCategoryInfo
    {
        [DataMember(Name = "name", EmitDefaultValue = false, Order = 10)]
        public string? Name { get; set; }

        [DataMember(Name = "alternativeNames", EmitDefaultValue = false, Order = 11)]
        public List<string?>? AlternativeNames { get; set; }

        [DataMember(Name = "rules", EmitDefaultValue = false, Order = 100)]
        public List<Rule?>? Rules { get; set; }

        [DataMember(Name = "rulePatterns", EmitDefaultValue = false, Order = 110)]
        public List<RulePattern?>? RulePatterns { get; set; }

        protected internal string? categoryString;

        [DataMember(Name = "category", Order = 150)]
        public virtual string? CategoryString
        {
            get
            {
                return categoryString;
            }
            set
            {
                categoryString = value;
                category = RuleCategoryParser.Instance.Parse(value);
            }
        }

        protected internal RuleCategory category = RuleCategory.Undefined;
        public virtual RuleCategory Category
        {
            get
            {
                return category;
            }
            set
            {
                category = value;
                CategoryString = category.ToString();
            }
        }
    }
}
