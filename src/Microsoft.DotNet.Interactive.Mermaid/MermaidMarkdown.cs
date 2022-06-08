// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Mermaid;

[TypeFormatterSource(typeof(MermaidMarkdownFormatter))]
public class MermaidMarkdown
{
    internal string Background { get; set; }
    internal string Width { get; set; }
    internal string Height { get; set; }
    public override string ToString()
    {
        return _value;
    }

    private readonly string _value;

    public MermaidMarkdown(string value)
    {
        Background = "white";
        Width = string.Empty;
        Height = string.Empty;
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }
}