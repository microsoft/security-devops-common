// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using System;

    public interface IRuleCategoryInfo
    {
        string? CategoryString { get; set; }
        RuleCategory Category { get; set; }
    }
}
