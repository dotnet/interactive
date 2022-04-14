// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.DotNet.Interactive.CSharpProject
{
    public class SourceFile
    {
        public SourceText Text { get; }

        public string Name { get; }

        private SourceFile(SourceText text, string name)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Name = name;
        }

        public static SourceFile Create(string text, string name) => new(SourceText.From(text ?? string.Empty), name: name);
    }
}
