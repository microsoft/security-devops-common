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

        [Fact]
        [Trait("Category", "Functional")]
        public void GetTemplateAnalyzerCategory()
        {
            var ruleQuery = new RuleQuery()
            {
                AnalyzerName = "template-analyzer"
            };

            RuleCategory actual = RulesDatabase.GetCategoryEnum(ruleQuery);
            RuleCategory expected = RuleCategory.IaC;

            Assert.Equal(expected, actual);
        }
    }
}
