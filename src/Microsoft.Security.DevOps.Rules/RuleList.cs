// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using System;
    using System.Collections.Generic;

    public class RuleList : List<Rule?>
    {
        public Rule? FindById(string ruleId)
        {
            return this.Find(rule => string.Equals(rule?.Id, ruleId));
        }
    }
}
