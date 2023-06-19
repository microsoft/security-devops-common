// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using Microsoft.Security.DevOps.Rules.Model;

    public class RuleCategoryParserTests
    {
        [Theory]
        [InlineData(null, RuleCategory.Undefined)]
        [InlineData("", RuleCategory.Undefined)]
        [InlineData("   ", RuleCategory.Undefined)]
        [InlineData("Custom", RuleCategory.Custom)]
        [InlineData("Category.fake", RuleCategory.Custom)]
        [InlineData("Code", RuleCategory.Code)]
        [InlineData("code", RuleCategory.Code)]
        [InlineData("Artifacts", RuleCategory.Artifacts)]
        [InlineData("artifacts", RuleCategory.Artifacts)]
        [InlineData("Artifact", RuleCategory.Artifacts)]
        [InlineData("artifact", RuleCategory.Artifacts)]
        [InlineData("Dependencies", RuleCategory.Dependencies)]
        [InlineData("dependencies", RuleCategory.Dependencies)]
        [InlineData("Dependency", RuleCategory.Dependencies)]
        [InlineData("dependency", RuleCategory.Dependencies)]
        [InlineData("Secrets", RuleCategory.Secrets)]
        [InlineData("secrets", RuleCategory.Secrets)]
        [InlineData("Secret", RuleCategory.Secrets)]
        [InlineData("secret", RuleCategory.Secrets)]
        [InlineData("IaC", RuleCategory.IaC)]
        [InlineData("iac", RuleCategory.IaC)]
        [InlineData("InfrastructureAsCode", RuleCategory.IaC)]
        [InlineData("infrastructureascode", RuleCategory.IaC)]
        [InlineData("infrastructure-as-code", RuleCategory.IaC)]
        [InlineData("Infrastructure-As-Code", RuleCategory.IaC)]
        [InlineData("infrastructure as code", RuleCategory.IaC)]
        [InlineData("Infrastructure As Code", RuleCategory.IaC)]
        [InlineData("Containers", RuleCategory.Containers)]
        [InlineData("containers", RuleCategory.Containers)]
        [InlineData("Container", RuleCategory.Containers)]
        [InlineData("container", RuleCategory.Containers)]
        [InlineData("APIs", RuleCategory.APIs)]
        [InlineData("apis", RuleCategory.APIs)]
        [Trait("Category", "Unit")]
        public void Parse(string? categoryString, RuleCategory expected)
        {
            RuleCategory actual = RuleCategoryParser.Instance.Parse(categoryString);

            Assert.Equal(expected, actual);
        }
    }
}