// /********************************************************
// *                                                       *
// *   Copyright (C) Microsoft. All rights reserved.       *
// *                                                       *
// ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using System;

    public class EmbeddedResourceReadException : Exception
    {
        public string? ResourceName { get; set; }

        public EmbeddedResourceReadException(string? resourceName)
            : base(string.Format(Messages.EmbeddedResourceReadException, resourceName ?? Messages.Unknown))
        {
            ResourceName = resourceName;
        }
    }
}
