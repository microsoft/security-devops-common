// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using Microsoft.Security.DevOps.Rules.Model;
    using System;

    /// <summary>
    /// Parses <see cref="RuleCategory"/> enums from strings.
    /// </summary>
    public class RuleCategoryParser : IRuleCategoryParser
    {
        private static RuleCategoryParser? instance;

        /// <summary>
        /// A singleton instance of the <see cref="RuleCategoryParser"/>.
        /// </summary>
        /// <remarks>
        /// Aids in testing and can be statically referenced from data contracts.
        /// </remarks>
        public static RuleCategoryParser Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new RuleCategoryParser();
                }

                return instance;
            }
            set
            {
                instance = value;
            }
        }

        private static Dictionary<string, RuleCategory>? categoryMap;
        private static Dictionary<string, RuleCategory> CategoryMap
        {
            get
            {
                if (categoryMap == null)
                {
                    categoryMap = new Dictionary<string, RuleCategory>(StringComparer.OrdinalIgnoreCase)
                    {
                        { "Artifact", RuleCategory.Artifacts },
                        { "Dependency", RuleCategory.Dependencies },
                        { "Secret", RuleCategory.Secrets },
                        { "InfrastructureAsCode", RuleCategory.IaC },
                        { "Infrastructure-As-Code", RuleCategory.IaC },
                        { "Infrastructure As Code", RuleCategory.IaC },
                        { "Container", RuleCategory.Containers },
                        { "APIs", RuleCategory.APIs }
                    };
                }

                return categoryMap;
            }
        }

        /// <summary>
        /// Parses a <see cref="RuleCateogry"/> from an input string.
        /// </summary>
        /// <param name="categoryString"></param>
        /// Undefined - string is null or whitespace
        /// Custom - the string was not recognized as an enum value
        /// 
        /// {EnumValue} - string case-insensitive matched a <see cref="RuleCategory"/> enum value
        /// 
        /// It attempt to parse additional values, case-insensitive, such as:
        /// InfrastructureAsCode => IaC
        /// Infrastrucutre-as-code => IAc
        /// Infrastructure as Code => IaC
        /// Artifact => Artifacts
        /// Dependency => Dependencies
        /// Container => Containers<returns>
        /// </returns>
        public RuleCategory Parse(string? categoryString)
        {
            RuleCategory category = RuleCategory.Undefined;

            if (!string.IsNullOrWhiteSpace(categoryString)
                && !Enum.TryParse(categoryString, true, out category)
                && !CategoryMap.TryGetValue(categoryString, out category))
            {
                category = RuleCategory.Custom;
            }

            return category;
        }
    }
}
