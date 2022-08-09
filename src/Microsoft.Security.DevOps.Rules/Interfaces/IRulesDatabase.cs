// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using System;

    public interface IRulesDatabase
    {
        string GetCategoryString(RuleQuery? query);
        //public RuleCategory GetCategoryEnum(string toolName, string ruleId);
        //public IRuleCategoryInfo GetCategoryInfo(string toolName, string ruleId);
    }
}
