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
        [InlineData("template-analyzer", null, RuleCategory.IaC)]
        [InlineData("template-analyzer", "ruleId.fake", RuleCategory.IaC)]
        [InlineData("TemplateAnalyzer", null, RuleCategory.IaC)]
        [InlineData("credscan", null, RuleCategory.Secrets)]
        [InlineData("credscan", "ruleId.fake", RuleCategory.Secrets)]
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
