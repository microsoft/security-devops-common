// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules.Interfaces
{
    using Microsoft.Security.DevOps.Rules.Model;
    using System;

    public interface IRuleCategoryInfo
    {
        string? CategoryString { get; set; }
        RuleCategory Category { get; set; }
    }
}
