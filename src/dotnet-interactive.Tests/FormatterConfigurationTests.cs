// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Formatting;
using Pocket;
using Formatter = Microsoft.DotNet.Interactive.Formatting.Formatter;

namespace Microsoft.DotNet.Interactive.App.Tests;

[TestClass]
public class FormatterConfigurationTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public FormatterConfigurationTests()
    {
        _disposables.Add(new AssertionScope());
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    [TestMethod]
    public void LatexString_type_is_formatted()
    {
        var latex = new LaTeXString(@"F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx");

        var mimeType = Formatter.GetPreferredMimeTypesFor(latex.GetType()).FirstOrDefault();

        var formattedValue = new FormattedValue(
            mimeType,
            latex.ToDisplayString(mimeType));

        formattedValue.MimeType.Should().Be("text/latex");
        formattedValue.Value.Should().Be(@"F(k) = \int_{-\infty}^{\infty} f(x) e^{2\pi i k} dx");
    }

    [TestMethod]
    public void ScriptContent_type_is_formatted()
    {
        var script = new ScriptContent("alert('hello');");
        var mimeType = Formatter.GetPreferredMimeTypesFor(script.GetType()).FirstOrDefault();

        var formattedValue = new FormattedValue(
            mimeType,
            script.ToDisplayString(mimeType));

        formattedValue.MimeType.Should().Be("text/html");
        formattedValue.Value.Should().Be(
            """
            <script type="text/javascript">alert('hello');</script>
            """);
    }

    [TestMethod]
    public void ScriptContent_type_with_possible_html_characters_is_not_HTML_encoded()
    {
        var scriptText = "if (true && false) { alert('hello with embedded <>\" escapes'); };";
        var script = new ScriptContent(scriptText);
        var mimeType = Formatter.GetPreferredMimeTypesFor(script.GetType()).FirstOrDefault();

        var formattedValue = new FormattedValue(
            mimeType,
            script.ToDisplayString(mimeType));

        formattedValue.MimeType.Should().Be("text/html");
        formattedValue.Value.Should().Be(
            $"""
             <script type="text/javascript">{scriptText}</script>
             """);
    }
}