// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive;

public record KernelCommandInfo(string Name);

public class KernelDirectiveInfo
{
    public KernelDirectiveInfo(string Name, bool IsKernelSpecifier)
    {
        this.Name = Name;
        this.IsKernelSpecifier = IsKernelSpecifier;
    }

    public string Name { get; init; }

    public bool IsKernelSpecifier { get; init; }

}

public class KernelInfo
{
    private readonly HashSet<KernelCommandInfo> _supportedKernelCommands = new();
    private readonly NamedSymbolCollection<KernelDirective> _supportedDirectives;
    private string? _displayName;

    [JsonConstructor]
    public KernelInfo(
        string localName, 
        string[]? aliases = null, 
        bool isProxy = false, 
        bool isComposite = false, 
        string? description = null)
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
        NameAndAliases = new HashSet<string> { LocalName };
        Uri = new Uri($"kernel://local/{LocalName}");

        if (aliases is not null)
        {
            NameAndAliases.UnionWith(aliases);
        }

        IsProxy = isProxy;
        IsComposite = isComposite;
        Description = description;

        _supportedDirectives = new NamedSymbolCollection<KernelDirective>(
            directive => directive.Name,
            onAdding: (directive, _) =>
            {
                directive.ParentKernelInfo = this;
            });
    }

    private string CreateDisplayName()
    {
        if (string.IsNullOrWhiteSpace(LanguageName))
        {
            return LocalName;
        }

        return $"{LocalName} - {LanguageName}";
    }

    public string[] Aliases
    {
        get => NameAndAliases.Where(n => n != LocalName).ToArray();
        init => NameAndAliases.UnionWith(value);
    }

    public string? LanguageName { get; set; }

    public string? LanguageVersion { get; set; }

    public bool IsProxy { get;  set; }

    public bool IsComposite { get;  set; }

    public string DisplayName
    {
        get => _displayName ?? CreateDisplayName();
        set => _displayName = value;
    }

    public string LocalName { get; }

    public Uri Uri { get; set; }

    public Uri? RemoteUri { get; set; }

    public string? Description { get; set; }

    public ICollection<KernelCommandInfo> SupportedKernelCommands
    {
        get => _supportedKernelCommands;
        init
        {
            if (value is null)
            {
                return;
            }

            _supportedKernelCommands.UnionWith(value);
        }
    }

    public ICollection<KernelDirective> SupportedDirectives
    {
        get => _supportedDirectives;
        init
        {
            if (value is null)
            {
                return;
            }

            foreach (var directive in value)
            {
                _supportedDirectives.Add(directive);
            }
        }
    }

    public override string ToString() => LocalName +
                                         (Uri is { } uri
                                              ? $" ({uri})"
                                              : null);

    internal HashSet<string> NameAndAliases { get; }

    internal bool SupportsCommand(string commandName) =>
        _supportedKernelCommands.Contains(new(commandName));

    internal void UpdateSupportedKernelCommandsFrom(KernelInfo source) =>
        _supportedKernelCommands.UnionWith(source.SupportedKernelCommands);

    internal bool TryGetDirective(string name, [MaybeNullWhen(false)] out KernelDirective directive) => 
        _supportedDirectives.TryGetValue(name, out directive);
}