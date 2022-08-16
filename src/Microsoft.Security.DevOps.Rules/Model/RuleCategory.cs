// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules.Model
{
    using System;

    /// <summary>
    /// The analysis category for the rule.
    /// 
    /// Undefined - Not set
    /// Custom - A category string was provided, but did not match one of these
    /// Code - Source code
    /// Artifacts - Build artifacts (built from source)
    /// Dependencies - Third party, referenced code or artifacts
    /// Secrets - Authentication and other privileged info
    /// IaC - Infrustrature as Code
    /// Containers - Container application environments
    /// </summary>
    /// <remarks>
    /// <see cref="RuleCategoryParser"/> will attempt to parse additional values, such as (case-insensitive):
    /// InfrastructureAsCode => IaC
    /// Infrastrucutre-as-code => IAc
    /// Infrastructure as Code => IaC
    /// Artifact => Artifacts
    /// Dependency => Dependencies
    /// Container => Containers
    /// </remarks>
    public enum RuleCategory
    {
        Undefined = 0,
        Custom = 1,
        Code = 2,
        Artifacts = 3,
        Dependencies = 4,
        Secrets = 5,
        IaC = 6,
        Containers = 7
    }
}
