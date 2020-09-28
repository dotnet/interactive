// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.App.CommandLine;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Http;
using Microsoft.DotNet.Interactive.Jupyter.Formatting;
using Newtonsoft.Json.Linq;
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
            var frontendEnvironment = new HtmlNotebookFrontedEnvironment(new Uri("http://12.12.12.12:4242"));
            

            CommandLineParser.SetUpFormatters(frontendEnvironment, new StartupOptions(), 1.Seconds());

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
            formattedValue.Value.Should().Be(@"<script type=""text/javascript"">alert('hello');</script>");
        }

        [Fact]
        public void ScriptContent_type_with_possible_html_characters_is_not_HTML_encoded()
        {
            var scriptText = "if (true && false) { alert('hello with embedded <>\" escapes'); };";
            var script = new ScriptContent(scriptText);
            var mimeType = Formatter.PreferredMimeTypeFor(script.GetType());

            var formattedValue = new FormattedValue(
                mimeType,
                script.ToDisplayString(mimeType));

            formattedValue.MimeType.Should().Be("text/html");
            formattedValue.Value.Should().Be($"<script type=\"text/javascript\">{scriptText}</script>");
        }

        [Fact]
        public void ScriptContent_type_is_wrapped_when_http_and_the_frontendEnvironment_is_JupyterFrontedEnvironment()
        {
            var frontendEnvironment = new HtmlNotebookFrontedEnvironment(new Uri("http://12.12.12.12:4242"));

            CommandLineParser.SetUpFormatters(frontendEnvironment, new StartupOptions(httpPort: new HttpPort(4242)), 5.Seconds());
            var script = new ScriptContent("alert('hello');");
            var mimeType = Formatter.PreferredMimeTypeFor(script.GetType());

            var formattedValue = new FormattedValue(
                mimeType,
                script.ToDisplayString(mimeType));

            formattedValue.MimeType.Should().Be("text/html");
            formattedValue.Value.Should().Be(@"<script type=""text/javascript"">if (typeof window.createDotnetInteractiveClient === typeof Function) {
createDotnetInteractiveClient('http://12.12.12.12:4242/').then(function (interactive) {
let notebookScope = getDotnetInteractiveScope('http://12.12.12.12:4242/');
alert('hello');
});
}</script>");
        }

        [Fact]
        public void js_wrapping_formatter_fails_if_apiUri_is_not_configured_within_the_configured_timeout()
        {
            var frontendEnvironment = new HtmlNotebookFrontedEnvironment();

            CommandLineParser.SetUpFormatters(frontendEnvironment, new StartupOptions(httpPort: new HttpPort(4242)), 1.Seconds());
            var script = new ScriptContent("alert('hello');");
            var mimeType = Formatter.PreferredMimeTypeFor(script.GetType());

            Action formatting = () =>  script.ToDisplayString(mimeType);
            formatting.Should()
                .Throw<TimeoutException>()
                .Which
                .Message
                .Should().Be("Timeout resolving the kernel's HTTP endpoint. Please try again.");
        }

        [Fact]
        public void JObject_is_formatted_as_application_json()
        {
            var obj = JObject.FromObject(new { value = 123, OtherValue = 456 });

            var mimeType = Formatter.PreferredMimeTypeFor(obj.GetType());
            var output = obj.ToDisplayString(JsonFormatter.MimeType);

            mimeType.Should().Be(JsonFormatter.MimeType);

            output
               .Should()
               .Be(@"{""value"":123,""OtherValue"":456}");
        }

        [Fact]
        public void JArray_is_formatted_as_application_json()
        {
            var obj = JArray.FromObject(new object[] { "one", 1 });

            var mimeType = Formatter.PreferredMimeTypeFor(obj.GetType());

            mimeType.Should().Be(JsonFormatter.MimeType);

            obj.ToDisplayString(JsonFormatter.MimeType)
               .Should()
               .Be(@"[""one"",1]");
        }
    }
}
