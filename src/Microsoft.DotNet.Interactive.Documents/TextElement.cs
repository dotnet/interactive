// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Documents;

public class TextElement : InteractiveDocumentOutputElement
{
    public TextElement(string? text, string? name = "stdout")
    {
        Text = text ?? "";
        Name = name ?? "stdout";
    }

    public string Name { get; }

    public string Text { get; }
}