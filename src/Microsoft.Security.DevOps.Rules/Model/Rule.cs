// /********************************************************
// *                                                       *
// *   Copyright (C) Microsoft. All rights reserved.       *
// *                                                       *
// ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using Microsoft.CodeAnalysis.Sarif;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// A SARIF <see cref="ReportingDescriptor"/> with additional fields, such as Category.
    /// </summary>
    /// <remarks>
    /// Fields added here should be attempted to be added to the SARIF schema.
    /// </remarks>
    [DataContract]
    public class Rule : ReportingDescriptor, IRuleCategoryInfo
    {
        public RuleCollection? Parent { get; set; }

        [DataMember(Name = "alternativeIds", EmitDefaultValue = false)]
        public List<string?>? AlternativeIds { get; set; }

        private string? categoryString;

        [DataMember(Name = "category", EmitDefaultValue = false)]
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

        private RuleCategory category = RuleCategory.Undefined;
        public RuleCategory Category
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
