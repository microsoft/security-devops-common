// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class PropertyBag
    {
        [DataMember(Name = "properties", EmitDefaultValue = false, Order = int.MaxValue)]
        public Dictionary<string, string?>? Properties { get; set; }
    }
}
