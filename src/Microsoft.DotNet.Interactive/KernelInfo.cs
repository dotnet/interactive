// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive
{
    public class KernelInfo
    {
        public override string ToString()
        {
            return LocalName;
        }

        public KernelInfo(string localName) : this(localName, Array.Empty<string>(), null)
        {

        }

        public KernelInfo(string localName, IReadOnlyCollection<string> aliases, KernelUri destination = null)
        {
            Validate(localName);
            LocalName = localName;
            aliases ??= Array.Empty<string>();
            foreach (var alias in aliases)
            {
                Validate(alias);
            }

            var distinctAliases = new HashSet<string>(aliases);
            Aliases = distinctAliases;
            Destination = destination;
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

        public string LocalName { get; }
        public KernelUri Origin { get; internal set; }
        public KernelUri Destination { get; internal set; }
    }
}