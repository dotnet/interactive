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
        private readonly IReadOnlyCollection<string> _aliases = Array.Empty<string>();

        public KernelInfo(string localName, string? languageName = null, string? languageVersion = null)
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
        }

        public IReadOnlyCollection<string> Aliases
        {
            get => _aliases;
            init => _aliases = value.Except(new[] { LocalName }).ToArray();
        }

        public string? LanguageName { get; } 

        public string? LanguageVersion { get; } 

        public string LocalName { get; }

        public Uri? OriginUri { get; set; }

        public Uri? DestinationUri { get; set; }

        public IReadOnlyCollection<KernelCommandInfo> SupportedKernelCommands { get; init; } = Array.Empty<KernelCommandInfo>();

        public IReadOnlyCollection<DirectiveInfo> SupportedDirectives { get; init; } = Array.Empty<DirectiveInfo>();

        public override string ToString() => LocalName;

        public static KernelInfo Create(Kernel kernel, IReadOnlyCollection<string>? aliases = null) =>
            new(kernel.Name)
            {
                Aliases = aliases ?? Array.Empty<string>(),
                SupportedDirectives = kernel.Directives.Select(d => new DirectiveInfo(d.Name)).ToArray(),
                SupportedKernelCommands = kernel.SupportedCommandTypes().Select(c => new KernelCommandInfo(c.Name)).ToArray()
            };
    }
}