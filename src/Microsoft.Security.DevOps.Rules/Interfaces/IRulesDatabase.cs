// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using Microsoft.Security.DevOps.Rules.Model;

    public interface IRulesDatabase
    {
        void Load();
        void Load(string filePath);

        QueryResult Query(RuleQuery? query);

        Rule? GetRule(RuleQuery? query);
        RuleCollection? GetAnalyzer(RuleQuery? query);
        RuleCollection? GetRuleset(RuleQuery? query);

        RuleCategory GetCategoryEnum(RuleQuery? query);
        string? GetCategoryString(RuleQuery? query);

    }
}
