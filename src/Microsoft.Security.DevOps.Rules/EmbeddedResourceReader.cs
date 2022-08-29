// /********************************************************
//  *                                                       *
//  *   Copyright (C) Microsoft. All rights reserved.       *
//  *                                                       *
//  ********************************************************/

namespace Microsoft.Security.DevOps.Rules
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;

    internal class EmbeddedResourceReader
    {
        /// <summary>
        /// Reads an embedded resource by name.
        /// </summary>
        public virtual T? Read<T>(string resourceName)
            where T : class
        {
            Assembly assembly = typeof(T).Assembly;
            T? embeddedResource = Read<T>(resourceName, assembly);

            return embeddedResource;
        }

        /// <summary>
        /// Reads an embedded resource by name from an assembly.
        /// </summary>
        public virtual T? Read<T>(string resourceName, Assembly assembly)
            where T : class
        {
            T? embeddedResource = null;
            var lines = ReadLines(resourceName, assembly)?.ToList();

            if (lines?.Any() == true)
            {
                string content = string.Join("\n", lines);
                embeddedResource = JsonConvert.DeserializeObject<T>(content);
            }

            return embeddedResource;
        }

        public virtual IEnumerable<string> ReadLines(string resourceName, Assembly assembly)
        {
            if (string.IsNullOrEmpty(resourceName))
            {
                throw new ArgumentNullException(nameof(resourceName));
            }

            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            using (Stream? resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                if (resourceStream == null)
                {
                    throw new EmbeddedResourceReadException(resourceName);
                }

                using (var reader = new StreamReader(resourceStream))
                {
                    string? line;

                    while ((line = reader?.ReadLine()) != null)
                    {
                        yield return line;
                    }
                }
            }
        }
    }
}
