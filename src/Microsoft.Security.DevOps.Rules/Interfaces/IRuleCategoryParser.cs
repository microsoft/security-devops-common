// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules.Interfaces
{
    using Microsoft.Security.DevOps.Rules.Model;

    public interface IRuleCategoryParser
    {
        RuleCategory Parse(string? categoryString);
    }
}
