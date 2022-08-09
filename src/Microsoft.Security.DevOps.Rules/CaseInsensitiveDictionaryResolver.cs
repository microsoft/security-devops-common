// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using System;
    using System.Collections.Generic;

    internal class CaseInsensitiveDictionaryResolver
    {
        private static CaseInsensitiveDictionaryResolver? instance;

        /// <summary>
        /// A singleton instance of the <see cref="RuleCategoryParser"/>.
        /// </summary>
        /// <remarks>
        /// Aids in testing and can be statically referenced from data contracts.
        /// </remarks>
        public static CaseInsensitiveDictionaryResolver Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CaseInsensitiveDictionaryResolver();
                }

                return instance;
            }
            set
            {
                instance = value;
            }
        }

        internal Dictionary<string, TValue>? Resolve<TValue>(Dictionary<string, TValue>? value)
        {
            if (value != null
                && value.Comparer != StringComparer.OrdinalIgnoreCase)
            {
                value = new Dictionary<string, TValue>(value, StringComparer.OrdinalIgnoreCase);
            }

            return value;
        }
    }
}
