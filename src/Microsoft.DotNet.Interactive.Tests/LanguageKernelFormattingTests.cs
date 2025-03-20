// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using static Microsoft.DotNet.Interactive.Formatting.Tests.Tags;

#pragma warning disable 8509
namespace Microsoft.DotNet.Interactive.Tests;

[TestClass]
public class LanguageKernelFormattingTests : LanguageKernelTestBase
{
    public LanguageKernelFormattingTests(TestContext output) : base(output)
    {
    }

    protected override CSharpKernel CreateCSharpKernel()
    {
        var cSharpKernel = base.CreateCSharpKernel();

        cSharpKernel.DeferCommand(new SubmitCode($@"
using static {typeof(PocketViewTags).FullName};
using {typeof(PocketView).Namespace};
"));

        return cSharpKernel;
    }

    [TestMethod]
    // PocketView
    [DataRow(Language.CSharp, "b(123)", $"<b>{PlainTextBegin}123{PlainTextEnd}</b>")]
    [DataRow(Language.FSharp, "b [] [str \"123\" ]", "<b>123</b>")]
    // sequence
    [DataRow(Language.CSharp, "new[] { 1, 2, 3, 4 }", "<pre>")]
    [DataRow(Language.FSharp, "[1; 2; 3; 4]", "<pre>")]
    // sequence of anonymous objects
    [DataRow(Language.CSharp, "new[] { new { a = 123 }, new { a = 456 } }", "<table>")]
    [DataRow(Language.FSharp, "[{| a = 123 |}; {| a = 456 |}]", "<div>")]
    public async Task Default_formatting_is_HTML(
        Language language,
        string submission,
        string expectedContent)
    {
        var kernel = CreateKernel(language, openTestingNamespaces: true);

        var result = await kernel.SendAsync(new SubmitCode(submission));

        result.Events
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v =>
                v.MimeType == "text/html" &&
                v.Value.ToString().Contains(expectedContent));
    }

    [TestMethod]
    [DataRow(Language.CSharp, "display(\"<test></test>\")", "<test></test>")]
    [DataRow(Language.FSharp, "display(\"<test></test>\")", "<test></test>")]
    public async Task String_is_rendered_as_plain_text_via_display(
        Language language,
        string submission,
        string expectedContent)
    {
        var kernel = CreateKernel(language, openTestingNamespaces: true);

        var result = await kernel.SendAsync(new SubmitCode(submission));

        result.Events
              .Should()
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v =>
                                 v.MimeType == "text/plain" &&
                                 v.Value.ToString().Contains(expectedContent));
    }

    [TestMethod]
    [DataRow(Language.CSharp, "\"hi\"", "hi")]
    [DataRow(Language.FSharp, "\"hi\"", "hi")]
    public async Task String_is_rendered_as_plain_text_via_implicit_return(
        Language language,
        string submission,
        string expectedContent)
    {
        var kernel = CreateKernel(language, openTestingNamespaces: true);

        var result = await kernel.SendAsync(new SubmitCode(submission));

        var valueProduced = result
            .Events
            .OfType<ReturnValueProduced>()
            .First();

        valueProduced
            .FormattedValues
            .Should()
            .ContainSingle(v =>
                v.MimeType == "text/plain" &&
                v.Value.ToString().Contains(expectedContent));
    }

    [TestMethod]
    [DataRow(Language.CSharp, "{ \"hello\": 123 ", "application/json")]
    [DataRow(Language.CSharp, "<span class=\"test\">hello!&nbsp;</span>", "text/html")]
    public async Task DisplayAs_renders_string_as_specified_mime_type(
        Language language,
        string stringValue,
        string mimeType)
    {
        var kernel = CreateKernel(language, openTestingNamespaces: true);

        await kernel.FindKernelByName("csharp").As<CSharpKernel>()
            .SendAsync(new SendValue(nameof(stringValue), stringValue, FormattedValue.CreateSingleFromObject(stringValue)));

        var code = $"stringValue.DisplayAs(\"{mimeType}\");";

        var result = await kernel.SendAsync(new SubmitCode(code));

        result
            .Events
            .Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v =>
                v.MimeType == mimeType &&
                v.Value == stringValue);
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task DisplayTable_produces_tabular_HTML_output_for_IEnumerable_T(Language language)
    {
        var kernel = CreateKernel(language, openTestingNamespaces: true);

        var code = language switch
        {
            Language.CSharp => """
            new[] { 
                new { Title = "Troll 2", Stars = 0.25 },
                new { Title = "The Room", Stars = 0.4 } }.DisplayTable();
            """,
            Language.FSharp => """
            type MovieRating = { Title: string; Stars: float }
            let ratings = 
                [ { Title = "Troll 2"; Stars = 0.25 };
                  { Title = "The Room"; Stars = 0.4 } ]
            ratings.DisplayTable()
            """
        };

        var result = await kernel.SendAsync(new SubmitCode(code));

        result.Events.Should().NotContainErrors();

        result.Events
              .Should().ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues.Should().ContainSingle(v => v.MimeType == "text/html")
              .Which
              .Value.Should().ContainEquivalentHtmlFragments("""
                <table>
                  <thead>
                      <tr>
                          <td><span>Title</span></td>
                          <td><span>Stars</span></td>
                      </tr>
                  </thead>
                  <tbody>
                      <tr>
                          <td>Troll 2</td>
                          <td>
                              <div class="dni-plaintext">
                                  <pre>0.25</pre>
                              </div>
                          </td>
                      </tr>
                      <tr>
                          <td>The Room</td>
                          <td>
                              <div class="dni-plaintext">
                                  <pre>0.4</pre>
                              </div>
                          </td>
                      </tr>
                  </tbody>
              </table>
              """);
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task display_can_be_called_without_specifying_class_name(Language language)
    {
        var kernel = CreateKernel(language, openTestingNamespaces: true);

        var submission = language switch
        {
            Language.CSharp => "display(b(\"hi!\"));",
            Language.FSharp => "display(b [] [ str \"hi!\" ]);",
        };

        await kernel.SendAsync(new SubmitCode(submission));

        KernelEvents
            .OfType<DisplayedValueProduced>()
            .SelectMany(v => v.FormattedValues)
            .Should()
            .ContainSingle(v =>
                v.MimeType == "text/html" &&
                v.Value.ToString().Contains("<b>"));
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task Displayed_value_can_be_updated(Language language)
    {
        var kernel = CreateKernel(language, openTestingNamespaces: true);

        var submission = language switch
        {
            Language.CSharp => "var d = display(b(\"hello\")); d.Update(b(\"world\"));",
            Language.FSharp => "let d = display(b [] [ str \"hello\"])\nd.Update(b [] [str \"world\"])",
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

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task Displayed_value_can_be_updated_from_later_submissions(Language language)
    {
        var kernel = CreateKernel(language, openTestingNamespaces: true);

        var submissions = language switch
        {
            Language.CSharp => new[] { "var d = display(b(\"hello\"));", "d.Update(b(\"world\"));" },
            Language.FSharp => new[] { "let d = display(b [] [ str \"hello\" ])", "d.Update(b [] [ str \"world\" ])" },
        };

        await kernel.SubmitCodeAsync(submissions[0]);

        var updateCommandResult = await kernel.SubmitCodeAsync(submissions[1]);

        updateCommandResult
            .Events
            .Should()
            .ContainSingle<DisplayedValueUpdated>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v =>
                v.MimeType == "text/html" &&
                v.Value.ToString().Contains("<b>world</b>"));
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task Value_display_and_update_are_in_right_order(Language language)
    {
        var kernel = CreateKernel(language, openTestingNamespaces: true);

        var submission = language switch
        {
            Language.CSharp => "var d = display(b(\"hello\")); d.Update(b(\"world\"));",
            Language.FSharp => "let d = display(b [] [ str \"hello\" ])\nd.Update(b [] [ str \"world\" ])",
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

    [TestMethod]
    [DataRow(Language.CSharp, "display(HTML(\"<b>hi!</b>\"));")]
    [DataRow(Language.FSharp, "display(HTML(\"<b>hi!</b>\"))")]
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

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
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

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task CSS_helper_emits_content_within_a_chunk_of_javascript(Language language)
    {
        var kernel = CreateKernel(language);

        var cssContent = "h1 { background: red; }";

        var submission = language switch
        {
            Language.CSharp => $@"CSS(""{cssContent}"");",
            Language.FSharp => $@"CSS(""{cssContent}"")",
        };

        var result = await kernel.SendAsync(new SubmitCode(submission));

        result.Events
              .Should()
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v =>
                                 v.MimeType == "text/html" &&
                                 v.Value.ToString().Contains($"var css = `{cssContent}`"));
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
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

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task Display_indicates_when_a_value_is_null(Language language)
    {
        var kernel = CreateKernel(language);

        var submission = language switch
        {
            Language.CSharp => "display(null);",
            Language.FSharp => "display(null)"
        };

        await kernel.SendAsync(new SubmitCode(submission));

        KernelEvents.Should().NotContainErrors();

        KernelEvents
            .Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v =>
                v.MimeType == "text/html" &&
                v.Value.ToString().Contains(Formatter.NullString.HtmlEncode().ToString()));
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task Null_return_value_is_formatted_as_null(Language language)
    {
        var kernel = CreateKernel(language);

        var submission = language switch
        {
            Language.CSharp => "null",
            Language.FSharp => "let o : obj = null\no"
        };

        await kernel.SendAsync(new SubmitCode(submission));

        KernelEvents.Should().NotContainErrors();

        KernelEvents
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v =>
                v.MimeType == "text/html" &&
                v.Value.ToString().Contains(Formatter.NullString.HtmlEncode().ToString()));
    }

    [TestMethod]
    public async Task FSharpKernel_does_not_publish_return_values_for_unit()
    {
        var kernel = CreateKernel(Language.FSharp);

        await kernel.SubmitCodeAsync("\"Hello from F#!\" |> Console.WriteLine");

        KernelEvents.Should()
            .NotContain(e => e is ReturnValueProduced);
    }

    [TestMethod]
    public async Task FSharpKernel_opens_System()
    {
        var kernel = CreateKernel(Language.FSharp);

        await kernel.SubmitCodeAsync("Console.WriteLine(\"abc.fs\")");

        KernelEvents.Should()
            .NotContain(e => e is CommandFailed);
    }

    [TestMethod]
    public async Task FSharpKernel_opens_System_IO()
    {
        var kernel = CreateKernel(Language.FSharp);

        await kernel.SubmitCodeAsync("let t = Path.GetFileNameWithoutExtension(\"abc.fs\")");

        KernelEvents.Should()
            .Contain(e => e is CommandSucceeded);
    }

    [TestMethod]
    public async Task FSharpKernel_opens_System_Text()
    {
        var kernel = CreateKernel(Language.FSharp);

        var result = await kernel.SubmitCodeAsync("let t = StringBuilder()");

        result.Events
              .Should()
              .ContainSingle<CommandSucceeded>();
    }

    [TestMethod]
    public async Task FSharpKernel_does_not_open_System_Linq()
    {
        var kernel = CreateKernel(Language.FSharp);

        await kernel.SubmitCodeAsync("let t = Enumerable.Range(0,20)");

        KernelEvents.Should()
            .Contain(e => e is CommandFailed);
    }

    [TestMethod]
    public async Task FSharpKernel_does_not_open_System_Threading_Tasks()
    {
        var kernel = CreateKernel(Language.FSharp);

        await kernel.SubmitCodeAsync("let t : Task<int> = Unchecked.defaultof<_>");

        KernelEvents.Should()
            .Contain(e => e is CommandFailed);
    }

    [TestMethod]
    public async Task FSharpKernel_does_not_open_HTML_DSL()
    {
        var kernel = CreateKernel(Language.FSharp);

        await kernel.SubmitCodeAsync("let x = p [] []");

        KernelEvents.Should()
            .Contain(e => e is CommandFailed);
    }
}