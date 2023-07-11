// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using Microsoft.Security.DevOps.Rules.Model;
    using System;

    public class RulesDatabaseFunctionalTests
    {
        private RulesDatabase RulesDatabase = new RulesDatabase();

        [Theory]
        [InlineData("credscan", null, RuleCategory.Secrets)]
        [InlineData("credscan", "ruleId.fake", RuleCategory.Secrets)]
        [InlineData("templateanalyzer", null, RuleCategory.IaC)]
        [InlineData("templateanalyzer", "ruleId.fake", RuleCategory.IaC)]
        [InlineData("template-analyzer", null, RuleCategory.IaC)] // alternative name
        [InlineData("terrascan", null, RuleCategory.IaC)]
        [InlineData("terrascan", "ruleId.fake", RuleCategory.IaC)]
        [InlineData("42Crunch REST API Static Security Testing", null, RuleCategory.APIs)]
        [InlineData("42Crunch REST API Static Security Testing", "ruleId.fake", RuleCategory.APIs)]
        [InlineData("ADOScanner", null, RuleCategory.Hardening)]
        [InlineData("adoscanner", "ruleId.fake", RuleCategory.Hardening)]
        [InlineData("GitHubScanner", null, RuleCategory.Hardening)]
        [InlineData("githubscanner", "ruleId.fake", RuleCategory.Hardening)]
        [Trait("Category", "Functional")]
        public void GetTemplateAnalyzerCategory(string analyzerName, string ruleId, RuleCategory expected)
        {
            var ruleQuery = new RuleQuery()
            {
                AnalyzerName = analyzerName,
                RuleId = ruleId // Non existant
            };

            RuleCategory actual = RulesDatabase.GetCategoryEnum(ruleQuery);

            Assert.Equal(expected, actual);
        }
    }
}
