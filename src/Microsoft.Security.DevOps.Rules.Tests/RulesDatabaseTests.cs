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
    using System.Diagnostics.CodeAnalysis;
    using System.IO;

    public class RulesDatabaseTests : TestBase<RulesDatabase>
    {
        [AllowNull]
        private RulesFile RulesFile;

        private void SetRulesFile()
        {
            RulesFile = new RulesFile();

            MockObject
                .SetupGet(mock => mock.RulesFile)
                .Returns(RulesFile);
        }

        #region RulesFile:get

        [Fact]
        [Trait("Category", "Unit")]
        public void RulesFile_get()
        {
            SetupCallBase(mock => mock.RulesFile);

            RulesFile actual = Mocked.RulesFile;

            // It doesn't actually set rulesFile
            Assert.Null(actual);

            // It calls load
            MockObject.Verify(mock => mock.Load(), Times.Once);
        }

        #endregion RulesFile:get

        #region void Load()

        [Fact]
        [Trait("Category", "Unit")]
        public void Load()
        {
            SetupCallBase(mock => mock.Load());

            Mocked.Load();
        }

        #endregion void Load()

        #region void Load(string filePath)

        [Fact]
        [Trait("Category", "Unit")]
        public void Load_FilePath()
        {
            SetupCallBase(mock => mock.Load(It.IsAny<string>()));

            Mocked.Load("filePath.fake");

            MockObject.Verify(mock => mock.Read<RulesFile>("filePath.fake"), Times.Once);
        }

        #endregion void Load(string filePath)

        // T? Read<T>(string filePath)
        // Covered by functional test

        // TODO: Rule? GetRule(RuleQuery? query)

        #region RuleCollection? GetAnalyzer(RuleQuery? query)

        private void SetupGetAnalyzer(
            out RuleQuery query,
            out RuleCollection? expected)
        {
            SetupCallBase(mock => mock.GetAnalyzer(It.IsAny<RuleQuery?>()));

            query = new RuleQuery()
            {
                AnalyzerName = "AnalyzerName.fake"
            };

            expected = new RuleCollection()
            {
                Name = "GetAnalyzer"
            };

            SetRulesFile();
            RulesFile.Analyzers = new List<RuleCollection?>()
            {
                expected
            };

            MockObject
                .Setup(mock => mock.FindRuleCollectionByName(It.IsAny<string>(), It.IsAny<List<RuleCollection?>?>(), It.IsAny<bool>()))
                .Returns(expected);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetAnalyzer()
        {
            SetupGetAnalyzer(
                out RuleQuery query,
                out RuleCollection? expected);

            RuleCollection? actual = Mocked.GetAnalyzer(query);

            Assert.Equal(expected, actual);

            MockObject.Verify(mock => mock.FindRuleCollectionByName("AnalyzerName.fake", RulesFile.Analyzers, true));
        }

        #endregion RuleCollection? GetAnalyzer(RuleQuery? query)

        #region RuleCollection? GetRuleset(RuleQuery? query)

        private void SetupGetRuleset(
            out RuleQuery query,
            out RuleCollection? expected)
        {
            SetupCallBase(mock => mock.GetRuleset(It.IsAny<RuleQuery?>()));

            query = new RuleQuery()
            {
                RulesetName = "RulesetName.fake"
            };

            expected = new RuleCollection()
            {
                Name = "GetRuleset"
            };

            SetRulesFile();
            RulesFile.Rulesets = new List<RuleCollection?>()
            {
                expected
            };

            MockObject
                .Setup(mock => mock.FindRuleCollectionByName(It.IsAny<string>(), It.IsAny<List<RuleCollection?>?>(), It.IsAny<bool>()))
                .Returns(expected);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void GetRuleset()
        {
            SetupGetRuleset(
                out RuleQuery query,
                out RuleCollection? expected);

            RuleCollection? actual = Mocked.GetRuleset(query);

            Assert.Equal(expected, actual);

            MockObject.Verify(mock => mock.FindRuleCollectionByName("RulesetName.fake", RulesFile.Rulesets, true));
        }

        #endregion RuleCollection? GetRuleset(RuleQuery? query)

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
        [Trait("Category", "Unit")]
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
        [Trait("Category", "Unit")]
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

        #region void Validate([NotNull] RuleQuery? query)

        private void SetupValidate(
            out RuleQuery query)
        {
            SetupCallBase(mock => mock.Validate(It.IsAny<RuleQuery?>()));

            query = new RuleQuery();
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Validate_Null()
        {
            SetupValidate(
                out RuleQuery query);

            query = null;

            Assert.Throws<ArgumentNullException>("query", () => Mocked.Validate(query));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Validate_RuleId()
        {
            SetupValidate(
                out RuleQuery query);

            query.RuleId = "RuleId.fake";
            Assert.Equal(QueryType.Undefined, query.Type);

            Mocked.Validate(query);

            Assert.Equal(QueryType.FindRule, query.Type);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Validate_AnalyzerName()
        {
            SetupValidate(
                out RuleQuery query);

            query.AnalyzerName = "AnalyzerName.fake";
            Assert.Equal(QueryType.Undefined, query.Type);

            Mocked.Validate(query);

            Assert.Equal(QueryType.FindAnalyzer, query.Type);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Validate_RulesetName()
        {
            SetupValidate(
                out RuleQuery query);

            query.RulesetName = "RulesetName.fake";
            Assert.Equal(QueryType.Undefined, query.Type);

            Mocked.Validate(query);

            Assert.Equal(QueryType.FindRuleset, query.Type);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Validate_RuleQueryInsufficientArgumentsException()
        {
            SetupValidate(
                out RuleQuery query);

            Assert.Throws<RuleQueryInsufficientArgumentsException>(() => Mocked.Validate(query));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void Validate_All()
        {
            SetupValidate(
                out RuleQuery query);

            query.RuleId = "RuleId.fake";
            query.AnalyzerName = "AnalyzerName.fake";
            query.RulesetName = "RulesetName.fake";
            query.All = true;
            Assert.Equal(QueryType.Undefined, query.Type);

            Mocked.Validate(query);

            Assert.Equal(QueryType.All, query.Type);
        }

        #endregion void Validate([NotNull] RuleQuery? query)
    }
}
