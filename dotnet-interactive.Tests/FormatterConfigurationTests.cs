// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter.Formatting;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class FormatterConfigurationTests
    {
        public FormatterConfigurationTests()
        {
            var frontendEnvironment = new BrowserFrontendEnvironment{
                ApiUri = new Uri("http://12.12.12.12:4242")
            };

            CommandLineParser.SetUpFormatters(frontendEnvironment);
        }

        [Fact]
        public void LatexString_type_is_formatted()
        {
            var latex = new LaTeXString(@"F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx");
            
            var mimeType = Formatter.PreferredMimeTypeFor(latex.GetType());

            var formattedValue = new FormattedValue(
                mimeType,
                latex.ToDisplayString(mimeType));

            formattedValue.MimeType.Should().Be("text/latex");
            formattedValue.Value.Should().Be(@"F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx");
        }

        [Fact]
        public void MathString_type_is_formatted()
        {
            var latex = new MathString(@"F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx");

            var mimeType = Formatter.PreferredMimeTypeFor(latex.GetType());

            var formattedValue = new FormattedValue(
                mimeType,
                latex.ToDisplayString(mimeType));

            formattedValue.MimeType.Should().Be("text/latex");
            formattedValue.Value.Should().Be(@"$$F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx$$");
        }

        [Fact]
        public void ScriptContent_type_is_formatted()
        {
            var script = new ScriptContent("alert('hello');");
            var mimeType = Formatter.PreferredMimeTypeFor(script.GetType());

            var formattedValue = new FormattedValue(
                mimeType,
                script.ToDisplayString(mimeType));

            formattedValue.MimeType.Should().Be("text/html");
            formattedValue.Value.Should().Be($@"<script type=""text/javascript"">createDotnetInteractiveClient('http://12.12.12.12:4242/').then(function (interactive) {{
let notebookScope = getDotnetInteractiveScope('http://12.12.12.12:4242/');
alert('hello');
}});</script>");
        }
    }
}