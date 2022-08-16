// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using Microsoft.Security.DevOps.Rules.Model;
    using SuperTestBase;
    using System;
    using System.ComponentModel;
    using System.IO;

    public class RulesDatabaseTests : TestBase<RulesDatabase>
    {
        #region RulesFile:get

        [Fact]
        [Category("Unit")]
        public void RulesFile_get()
        {
            RulesFile actual = Mocked.RulesFile;

            // It doesn't actually set rulesFile
            Assert.Null(actual);

            // It calls load
            MockObject.Verify(mock => mock.Load(), Times.Once);
        }

        #endregion RulesFile:get

        #region void Load()

        private void SetupLoad()
        {
            SetupCallBase(mock => mock.Load());
        }

        [Fact]
        [Category("Unit")]
        public void Load()
        {
            SetupLoad();

            Mocked.Load();

            string expected = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rules.json"); ;
            MockObject.Verify(mock => mock.Load(expected), Times.Once);
        }

        #endregion void Load()

        #region void Load(string filePath)

        private void SetupLoadFilePath()
        {
            SetupCallBase(mock => mock.Load(It.IsAny<string>()));
        }

        [Fact]
        [Category("Unit")]
        public void Load_FilePath()
        {
            SetupLoadFilePath();

            Mocked.Load("filePath.fake");

            MockObject.Verify(mock => mock.Read<RulesFile>("filePath.fake"), Times.Once);
        }

        #endregion void Load(string filePath)

        // T? Read<T>(string filePath)
        // Covered by functional test

        #region RuleCategory GetCategoryEnum(RuleQuery? query)

        private void SetupGetCategoryEnum(
            out RuleQuery? ruleQuery,
            out QueryResult? result,
            bool resultIsNull = false,
            RuleCategory ruleCategory = RuleCategory.Custom)
        {
            SetupCallBase(mock => mock.GetCategoryEnum(It.IsAny<RuleQuery?>()));

            ruleQuery = new RuleQuery();
            result = resultIsNull ? null : new QueryResult(ruleQuery)
            {
                Category = ruleCategory
            };

            MockObject
                .Setup(mock => mock.Query(It.IsAny<RuleQuery?>()))
                .Returns(result);
        }

        [Theory]
        [InlineData(true, RuleCategory.Code, RuleCategory.Undefined)]
        [InlineData(false, RuleCategory.Artifacts, RuleCategory.Artifacts)]
        [Category("Unit")]
        public void GetCategoryEnum(bool resultIsNull, RuleCategory category, RuleCategory expected)
        {
            SetupGetCategoryEnum(
                out RuleQuery? ruleQuery,
                out QueryResult? result,
                resultIsNull,
                category);

            RuleCategory actual = Mocked.GetCategoryEnum(ruleQuery);

            Assert.Equal(expected, actual);

            MockObject.Verify(mock => mock.Query(ruleQuery), Times.Once);
        }

        #endregion RuleCategory GetCategoryEnum(RuleQuery? query)

        #region RuleCategory GetCategoryString(RuleQuery? query)

        private void SetupGetCategoryString(
            out RuleQuery? ruleQuery,
            out QueryResult? result,
            bool resultIsNull = false,
            string? categoryString = "CategoryString.fake")
        {
            SetupCallBase(mock => mock.GetCategoryString(It.IsAny<RuleQuery?>()));

            ruleQuery = new RuleQuery();
            result = resultIsNull ? null : new QueryResult(ruleQuery)
            {
                CategoryString = categoryString
            };

            MockObject
                .Setup(mock => mock.Query(It.IsAny<RuleQuery?>()))
                .Returns(result);
        }

        [Theory]
        [InlineData(true, "CategoryString.fake", null)]
        [InlineData(false, "CategoryString.fake", "CategoryString.fake")]
        [InlineData(false, null, null)]
        [Category("Unit")]
        public void GetCategoryString(bool resultIsNull, string? categoryString, string? expected)
        {
            SetupGetCategoryString(
                out RuleQuery? ruleQuery,
            out QueryResult? result,
                resultIsNull,
                categoryString);

            string? actual = Mocked.GetCategoryString(ruleQuery);

            Assert.Equal(expected, actual);

            MockObject.Verify(mock => mock.Query(ruleQuery), Times.Once);
        }

        #endregion RuleCategory GetCategoryString(RuleQuery? query)

        #region RuleCollection? GetAnalyzer(RuleQuery? query)

        private void SetupGetAnalyzer()
        {
            SetupCallBase(mock => mock.GetAnalyzer(It.IsAny<RuleQuery?>()));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetAnalyzer()
        {
            RuleQuery? query

            RuleCollection? actual = Mocked.GetAnalyzer();

            Mocked.FindRuleCollectionByName(Mocked.RulesFile.Analyzers, "AnalyzerName.fake", true);
        }

        #endregion RuleCollection? GetAnalyzer(RuleQuery? query)
    }
}
