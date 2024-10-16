// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Formatting;

[TypeFormatterSource(typeof(LaTeXStringFormatterSource))]
public class LaTeXString
{
    private readonly string _latexCode;

    public LaTeXString(string latexCode)
    {
        _latexCode = latexCode ?? throw new ArgumentNullException(nameof(latexCode));
    }

    public static implicit operator LaTeXString(string source) => new(source);

    public override string ToString()
    {
        return _latexCode;
    }
}