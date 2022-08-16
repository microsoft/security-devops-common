// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using System;

    public class RuleQueryInsufficientArgumentsException : Exception
    {
        public RuleQuery? RuleQuery { get; set; }

        public RuleQueryInsufficientArgumentsException(RuleQuery? query)
        {
            RuleQuery = query;
        }
    }
}
