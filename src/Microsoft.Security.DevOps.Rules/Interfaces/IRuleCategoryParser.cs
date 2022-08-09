// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    public interface IRuleCategoryParser
    {
        RuleCategory Parse(string? categoryString);
    }
}
