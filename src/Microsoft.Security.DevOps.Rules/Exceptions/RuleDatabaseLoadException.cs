// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using System;

    public class RuleDatabaseLoadException : Exception
    {
        public RuleDatabaseLoadException() : base(Messages.RulesDatabaseLoadException)
        {

        }
    }
}
