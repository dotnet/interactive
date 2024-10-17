// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public class LaTeXFormatterTests
{
    [Fact]
    public void Can_generate_LaTeX_string_from_LaTeX_object()
    {
        var latexSource = """
                          \begin{equation}
                          F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx
                          \end{equation}
                          """;

        new LaTeXString(latexSource)
            .ToDisplayString("text/latex")
            .Should()
            .Be(latexSource);
    }
}