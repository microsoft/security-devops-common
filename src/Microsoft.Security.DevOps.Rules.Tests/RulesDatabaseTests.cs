// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules.Tests
{
    using Moq.AutoMock;
    using System;

    public class RulesDatabaseTests
    {
        [Fact]
        public void Test()
        {
            var mocker = new AutoMocker();
            RulesDatabase mocked = mocker.CreateInstance<RulesDatabase>();
        }
    }
}
