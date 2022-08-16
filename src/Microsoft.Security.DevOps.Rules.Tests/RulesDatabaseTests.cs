// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using Microsoft.Security.DevOps.Rules;
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

        #region T? Read<T>(string filePath)

        private void SetupRead()
        {
            SetupCallBase(mock => mock.Read<RulesFile?>(It.IsAny<string>()));
        }

        [Fact]
        [Category("Unit")]
        public void Read()
        {
            SetupRead();

            RulesFile? actual = Mocked.Read<RulesFile?>("filePath.fake");
        }

        #endregion T? Read<T>(string filePath)

        #region IRuleCategoryInfo? GetCategoryInfo(RuleQuery? query)

        private void SetupGetCategoryInfo(
            out RuleQuery? ruleQuery,
            out IRuleCategoryInfo? ruleInfo)
        {
            SetupCallBase(mock => mock.GetCategoryInfo(It.IsAny<RuleQuery?>()));

            ruleQuery = new RuleQuery();
            var rule = new Rule();
            ruleInfo = rule;

            MockObject
                .Setup(mock => mock.GetRule(It.IsAny<RuleQuery?>()))
                .Returns(rule);
        }

        [Fact]
        [Category("Unit")]
        public void GetCategoryInfo()
        {
            SetupGetCategoryInfo(
                out RuleQuery? ruleQuery,
                out IRuleCategoryInfo? ruleInfo);

            IRuleCategoryInfo? actual = Mocked.GetCategoryInfo(ruleQuery);

            IRuleCategoryInfo? expected = new Rule();

            Assert.Equal(expected, actual);

            MockObject.Verify(mock => mock.GetRule(ruleQuery), Times.Once);
        }

        #endregion IRuleCategoryInfo? GetCategoryInfo(RuleQuery? query)

        #region RuleCategory GetCategoryEnum(RuleQuery? query)

        private void SetupGetCategoryEnum(
            out RuleQuery? ruleQuery,
            out Rule? rule,
            bool ruleIsNull = false,
            RuleCategory ruleCategory = RuleCategory.Custom)
        {
            SetupCallBase(mock => mock.GetCategoryEnum(It.IsAny<RuleQuery?>()));

            ruleQuery = new RuleQuery();
            rule = ruleIsNull ? null : new Rule()
            {
                Category = ruleCategory
            };

            MockObject
                .Setup(mock => mock.GetRule(It.IsAny<RuleQuery?>()))
                .Returns(rule);
        }

        [Theory]
        [InlineData(true, RuleCategory.Code, RuleCategory.Undefined)]
        [InlineData(false, RuleCategory.Artifacts, RuleCategory.Artifacts)]
        [Category("Unit")]
        public void GetCategoryEnum(bool ruleIsNull, RuleCategory category, RuleCategory expected)
        {
            SetupGetCategoryEnum(
                out RuleQuery? ruleQuery,
                out Rule? rule,
                ruleIsNull,
                category);

            RuleCategory actual = Mocked.GetCategoryEnum(ruleQuery);

            Assert.Equal(expected, actual);

            MockObject.Verify(mock => mock.GetRule(ruleQuery), Times.Once);
        }

        #endregion RuleCategory GetCategoryEnum(RuleQuery? query)

        #region RuleCategory GetCategoryString(RuleQuery? query)

        private void SetupGetCategoryString(
            out RuleQuery? ruleQuery,
            out Rule? rule,
            bool ruleIsNull = false,
            string? categoryString = "CategoryString.fake")
        {
            SetupCallBase(mock => mock.GetCategoryString(It.IsAny<RuleQuery?>()));

            ruleQuery = new RuleQuery();
            rule = ruleIsNull ? null : new Rule()
            {
                CategoryString = categoryString
            };

            MockObject
                .Setup(mock => mock.GetRule(It.IsAny<RuleQuery?>()))
                .Returns(rule);
        }

        [Theory]
        [InlineData(true, "CategoryString.fake", null)]
        [InlineData(false, "CategoryString.fake", "CategoryString.fake")]
        [InlineData(false, null, null)]
        [Category("Unit")]
        public void GetCategoryString(bool ruleIsNull, string? categoryString, string? expected)
        {
            SetupGetCategoryString(
                out RuleQuery? ruleQuery,
                out Rule? rule,
                ruleIsNull,
                categoryString);

            string? actual = Mocked.GetCategoryString(ruleQuery);

            Assert.Equal(expected, actual);

            MockObject.Verify(mock => mock.GetRule(ruleQuery), Times.Once);
        }

        #endregion RuleCategory GetCategoryString(RuleQuery? query)
    }
}
