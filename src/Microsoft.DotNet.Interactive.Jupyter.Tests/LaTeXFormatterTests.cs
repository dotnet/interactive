// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Formatting;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class LaTeXFormatterTests
    {
        [Fact]
        public void Can_generate_LaTeX_string_from_LaTeX_object()
        {
            Formatter<LaTeXString>.Register((laTeX, writer) =>
                {
                    writer.Write(laTeX.ToString());
                },
                "text/latex");

            var latexSource = @"\begin{equation}
F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx
\end{equation}";
            var laTex = new LaTeXString(latexSource);
            var writer = new StringWriter();
            laTex.FormatTo(writer, "text/latex");
            var s = writer.ToString();
            s.Should().Be(latexSource);
        }
    }

    public class MathFormatterTests
    {
        [Fact]
        public void Can_generate_LaTeX_string_from_LaTeX_object()
        {
            Formatter<MathString>.Register((math, writer) =>
                {
                    writer.Write(math.ToString());
                },
                "text/latex");

            var mathSource = @"F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx";
            var math = new MathString(mathSource);
            var writer = new StringWriter();
            math.FormatTo(writer, "text/latex");
            var s = writer.ToString();
            s.Should().Be($"$${mathSource}$$");
        }
    }
}