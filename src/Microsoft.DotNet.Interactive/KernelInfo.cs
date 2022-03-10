// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive
{
    public class KernelInfo
    {
        public KernelInfo(
            string localName,
            string? languageName = null,
            string? languageVersion = null,
            string[]? aliases = null)
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
            LanguageName = languageName;
            LanguageVersion = languageVersion;
            NameAndAliases = new HashSet<string> { LocalName };

            if (aliases is not null)
            {
                NameAndAliases.UnionWith(aliases);
            }
        }

        public string[] Aliases
        {
            get => NameAndAliases.Where(n => n != LocalName).ToArray();
            init => NameAndAliases.UnionWith(value);
        }

        public string? LanguageName { get; }

        public string? LanguageVersion { get; }

        public string LocalName { get; }

        public Uri? Uri { get; set; }

        public IReadOnlyCollection<KernelCommandInfo> SupportedKernelCommands { get; init; } = Array.Empty<KernelCommandInfo>();

        public IReadOnlyCollection<DirectiveInfo> SupportedDirectives { get; init; } = Array.Empty<DirectiveInfo>();

        public override string ToString() => LocalName;

        internal HashSet<string> NameAndAliases { get; }
    }
}