// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive
{
    public class KernelName
    {
        public override string ToString()
        {
            return Name;
        }

        public KernelName(string kernelName) : this(kernelName, Array.Empty<string>())
        {

        }

        public KernelName(string name, IReadOnlyCollection<string> aliases)
        {
            Validate(name);
            Name = name;

            foreach (var alias in aliases)
            {
                Validate(alias);
            }

            var distinctAliases = new HashSet<string>(aliases) { name };
            Aliases = distinctAliases;
        }

        private static void Validate(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value cannot be null or consist entirely of whitespace.");
            }

            if (name.StartsWith("#"))
            {
                throw new ArgumentException("Kernel names or aliases cannot begin with \"#\"");
            }
        }

        public IReadOnlyCollection<string> Aliases { get; }

        public string Name { get; }
    }
}