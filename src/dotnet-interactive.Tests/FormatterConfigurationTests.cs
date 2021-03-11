// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Jupyter.Formatting;
using Pocket;
using Xunit;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class FormatterConfigurationTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        public FormatterConfigurationTests()
        {
            var frontendEnvironment = new HtmlNotebookFrontendEnvironment(new Uri("http://12.12.12.12:4242"));

            CommandLineParser.SetUpFormatters(frontendEnvironment);

            _disposables.Add(Formatter.ResetToDefault);
            _disposables.Add(new AssertionScope());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact]
        public void LatexString_type_is_formatted()
        {
            var latex = new LaTeXString(@"F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx");

            var mimeType = Formatter.GetPreferredMimeTypeFor(latex.GetType());

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

            var mimeType = Formatter.GetPreferredMimeTypeFor(latex.GetType());

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
            var mimeType = Formatter.GetPreferredMimeTypeFor(script.GetType());

            var formattedValue = new FormattedValue(
                mimeType,
                script.ToDisplayString(mimeType));

            formattedValue.MimeType.Should().Be("text/html");
            formattedValue.Value.Should().Be(@"<script type=""text/javascript"">alert('hello');</script>");
        }

        [Fact]
        public void ScriptContent_type_with_possible_html_characters_is_not_HTML_encoded()
        {
            var scriptText = "if (true && false) { alert('hello with embedded <>\" escapes'); };";
            var script = new ScriptContent(scriptText);
            var mimeType = Formatter.GetPreferredMimeTypeFor(script.GetType());

            var formattedValue = new FormattedValue(
                mimeType,
                script.ToDisplayString(mimeType));

            formattedValue.MimeType.Should().Be("text/html");
            formattedValue.Value.Should().Be($"<script type=\"text/javascript\">{scriptText}</script>");
        }
    }
}
