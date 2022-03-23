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
        private readonly HashSet<KernelCommandInfo> _supportedKernelCommands = new();

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

        public Uri? RemoteUri { get; set; }

        public ICollection<KernelCommandInfo> SupportedKernelCommands
        {
            get => _supportedKernelCommands;
            init => _supportedKernelCommands.UnionWith(value ?? throw new ArgumentNullException(nameof(value)));
        }

        public IReadOnlyCollection<KernelDirectiveInfo> SupportedDirectives { get; init; } = Array.Empty<KernelDirectiveInfo>();

        public override string ToString() => LocalName +
                                             (Uri is { } uri
                                                  ? $" ({uri})"
                                                  : null);

        internal HashSet<string> NameAndAliases { get; }

        internal bool SupportsCommand(string commandName) =>
            _supportedKernelCommands.Contains(new(commandName));

        internal void UpdateFrom(KernelInfo source) =>
            _supportedKernelCommands.UnionWith(source.SupportedKernelCommands);
    }
}