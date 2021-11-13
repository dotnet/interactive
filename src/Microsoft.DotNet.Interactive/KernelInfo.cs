// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive
{
    public class KernelInfo
    {
        public KernelInfo(
            string localName,
            IReadOnlyCollection<string>? aliases = null,
            Uri? destinationUri = null)
        {
            if (string.IsNullOrWhiteSpace(localName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(localName));
            }

            if (localName.StartsWith("#"))
            {
                throw new ArgumentException("Kernel names or aliases cannot begin with \"#\"");
            }

            LocalName = localName;
            aliases ??= Array.Empty<string>();
            foreach (var alias in aliases)
            {
                if (string.IsNullOrWhiteSpace(alias))
                {
                    throw new ArgumentException("Value cannot be null or consist entirely of whitespace.");
                }

                if (alias.StartsWith("#"))
                {
                    throw new ArgumentException("Kernel names or aliases cannot begin with \"#\"");
                }
            }

            var distinctAliases = new HashSet<string>(aliases);
            Aliases = distinctAliases;
            DestinationUri = destinationUri;
        }

        public IReadOnlyCollection<string> Aliases { get; }

        public string LocalName { get; }

        public Uri? OriginUri { get; internal set; }

        public Uri? DestinationUri { get; internal set; }

        public override string ToString() => LocalName;

        public IReadOnlyCollection<string> CommandNames { get; }
        
        public IReadOnlyCollection<string> DirectiveNames { get; }

        public string Language { get; }
    }
}