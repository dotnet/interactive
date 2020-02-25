// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable 8509
namespace Microsoft.DotNet.Interactive.Tests
{
    [LogTestNamesToPocketLogger]
    public class LanguageKernelFormattingTests : LanguageKernelTestBase
    {
        public LanguageKernelFormattingTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory(Timeout = 45000)]
        // PocketView
        [InlineData(Language.CSharp, "b(123)", "<b>123</b>")]
        [InlineData(Language.FSharp, "b.innerHTML(123)", "<b>123</b>")]
        // sequence
        [InlineData(Language.CSharp, "new[] { 1, 2, 3, 4 }", "<table>")]
        [InlineData(Language.FSharp, "[1; 2; 3; 4]", "<table>")]
        // sequence of anonymous objects
        [InlineData(Language.CSharp, "new[] { new { a = 123 }, new { a = 456 } }", "<table>")]
        [InlineData(Language.FSharp, "[{| a = 123 |}; {| a = 456 |}]", "<table>")]
        public async Task Default_formatting_is_HTML(
            Language language,
            string submission,
            string expectedContent)
        {
            var kernel = CreateKernel(language);

            await kernel.SendAsync(new SubmitCode(submission));

            KernelEvents
                .Should()
                .ContainSingle<ReturnValueProduced>()
                .Which
                .FormattedValues
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Contains(expectedContent));
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp, "div(123).ToString()", "<div>123</div>")]
        [InlineData(Language.FSharp, "div.innerHTML(123).ToString()", "<div>123</div>")]
        [InlineData(Language.CSharp, "display(div(123).ToString());", "<div>123</div>")]
        [InlineData(Language.FSharp, "display(div.innerHTML(123).ToString())", "<div>123</div>")]
        [InlineData(Language.CSharp, "\"hi\"", "hi")]
        [InlineData(Language.FSharp, "\"hi\"", "hi")]
        public async Task String_is_rendered_as_plain_text(
            Language language,
            string submission,
            string expectedContent)
        {
            var kernel = CreateKernel(language);

            var result = await kernel.SendAsync(new SubmitCode(submission));

            var valueProduced = await result
                                      .KernelEvents
                                      .OfType<DisplayEventBase>()
                                      .Timeout(5.Seconds())
                                      .FirstAsync();

            valueProduced
                .FormattedValues
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/plain" &&
                                   v.Value.ToString().Contains(expectedContent));
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Display_helper_can_be_called_without_specifying_class_name(Language language)
        {
            var kernel = CreateKernel(language);

            var submission = language switch
            {
                Language.CSharp => "display(b(\"hi!\"));",
                Language.FSharp => "display(b.innerHTML(\"hi!\"));",
            };

            await kernel.SendAsync(new SubmitCode(submission));

            KernelEvents
                .OfType<DisplayedValueProduced>()
                .SelectMany(v => v.FormattedValues)
                .Should()
                .ContainSingle(v =>
                    v.MimeType == "text/html" &&
                    v.Value.ToString().Contains("<b>hi!</b>"));
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Displayed_value_can_be_updated(Language language)
        {
            var kernel = CreateKernel(language);

            var submission = language switch
            {
                Language.CSharp => "var d = display(b(\"hello\")); d.Update(b(\"world\"));",
                Language.FSharp => "let d = display(b.innerHTML(\"hello\"))\nd.Update(b.innerHTML(\"world\"))",
            };

            await kernel.SendAsync(new SubmitCode(submission));

            KernelEvents
                .OfType<DisplayedValueProduced>()
                .SelectMany(v => v.FormattedValues)
                .Should()
                .ContainSingle(v =>
                    v.MimeType == "text/html" &&
                    v.Value.ToString().Contains("<b>hello</b>"));


            KernelEvents
                .OfType<DisplayedValueUpdated>()
                .SelectMany(v => v.FormattedValues)
                .Should()
                .ContainSingle(v =>
                    v.MimeType == "text/html" &&
                    v.Value.ToString().Contains("<b>world</b>"));
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Value_display_and_update_are_in_right_order(Language language)
        {
            var kernel = CreateKernel(language);

            var submission = language switch
            {
                Language.CSharp => "var d = display(b(\"hello\")); d.Update(b(\"world\"));",
                Language.FSharp => "let d = display(b.innerHTML(\"hello\"))\nd.Update(b.innerHTML(\"world\"))",
            };

            await kernel.SendAsync(new SubmitCode(submission));

            var valueEvents =
                KernelEvents
                    .Where(e => e is DisplayedValueProduced || e is DisplayedValueUpdated)
                    .Select(e => e)
                   .ToList();

            valueEvents.First().Should().BeOfType<DisplayedValueProduced>();
            valueEvents.Last().Should().BeOfType<DisplayedValueUpdated>();
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp, "display(HTML(\"<b>hi!</b>\"));")]
        [InlineData(Language.FSharp, "display(HTML(\"<b>hi!</b>\"))")]
        public async Task HTML_helper_emits_HTML_which_is_not_encoded_and_has_the_text_html_mime_type(
            Language language, 
            string code)
        {
            var kernel = CreateKernel(language);

            var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync(code);

            events.Should().NotContainErrors();

            events.Should()
                  .ContainSingle<DisplayedValueProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle(f => f.Value.Equals("<b>hi!</b>") &&
                                      f.MimeType == "text/html");
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task Javascript_helper_emits_string_as_content_within_a_script_element(Language language)
        {
            var kernel = CreateKernel(language);

            var scriptContent = "alert('Hello World!');";

            var submission = language switch
            {
                Language.CSharp => $@"Javascript(""{scriptContent}"");",
                Language.FSharp => $@"Javascript(""{scriptContent}"")",
            };

            await kernel.SendAsync(new SubmitCode(submission));

            var formatted =
                KernelEvents
                    .OfType<DisplayedValueProduced>()
                    .SelectMany(v => v.FormattedValues)
                    .ToArray();

            formatted
                .Should()
                .ContainSingle(v =>
                                   v.MimeType == "text/html" &&
                                   v.Value.ToString().Contains($@"<script type=""text/javascript"">{scriptContent}</script>"));
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_displays_detailed_information_for_exceptions_thrown_in_user_code(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => new[]
                {
                    // F# syntax doesn't allow a bare `raise ...` expression at the root due to type inference being
                    // ambiguous, but the same effect can be achieved by wrapping the exception in a strongly-typed
                    // function call.
                    @"open System
let f (): unit = 
    try
        raise (Exception(""the-inner-exception""))
    with
        | ex -> raise (DataMisalignedException(""the-outer-exception"", ex))

f ()"
                },

                Language.CSharp => new[]
                {
                    @"
void f()
{
    try
    {
        throw new Exception(""the-inner-exception"");
    }
    catch(Exception e)
    {
        throw new DataMisalignedException(""the-outer-exception"", e);
    }
    
}

f();"
                }
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Contain("the-inner-exception")
                .And
                .Contain("the-outer-exception");
        }
    }
}