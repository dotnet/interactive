// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket.For.Xunit;
using Xunit;
using Xunit.Abstractions;
using DiagnosticsProduced = Microsoft.DotNet.Interactive.Events.DiagnosticsProduced;

#pragma warning disable 8509
#pragma warning disable 8524
namespace Microsoft.DotNet.Interactive.Tests;

[LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
public sealed class LanguageKernelTests : LanguageKernelTestBase
{
    public LanguageKernelTests(ITestOutputHelper output) : base(output)
    {
    }

    [Theory]
    [InlineData(Language.FSharp)]
    [InlineData(Language.CSharp)]
    public async Task it_fails_to_get_value_with_unsupported_mimetype(Language language)
    {
        var valueName = "x";
        var mimeType = "unsupported-mimeType";

        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.FSharp => new[]
            {
                $"let {valueName} = 123"
            },

            Language.CSharp => new[]
            {
                $"var {valueName} = 123;"
            }
        };

        await SubmitCode(kernel, source);

        var result = await kernel.SendAsync(new RequestValue(valueName, mimeType: mimeType, targetKernelName: language.LanguageName()));

        result.Events
              .Should()
              .ContainSingle<CommandFailed>()
              .Which
              .Exception
              .Should()
              .BeOfType<ArgumentException>()
              .Which
              .Message
              .Should()
              .Be($"No formatter is registered for MIME type {mimeType}.");
    }

    [Theory]
    [InlineData(Language.FSharp)]
    [InlineData(Language.CSharp)]
    public async Task it_returns_the_result_of_a_non_null_expression(Language language)
    {
        var kernel = CreateKernel(language);

        await SubmitCode(kernel, "123");

        KernelEvents
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .Value
            .Should()
            .Be(123);
    }

    [Theory]
    [InlineData(Language.FSharp)]
    public async Task it_returns_no_result_for_a_null_value(Language language)
    {
        var kernel = CreateKernel(language);

        await SubmitCode(kernel, "null");

        KernelEvents
            .Should()
            .NotContain(e => e is ReturnValueProduced);
    }

    // Option 1: inline switch
    [Theory]
    [InlineData(Language.FSharp)]
    [InlineData(Language.CSharp)]
    public async Task it_remembers_state_between_submissions(Language language)
    {
        var source = language switch
        {
            Language.FSharp => new[]
            {
                "let add x y = x + y",
                "add 2 3"
            },

            Language.CSharp => new[]
            {
                "int Add(int x, int y) { return x + y; }",
                "Add(2, 3)"
            }
        };

        var kernel = CreateKernel(language);

        await SubmitCode(kernel, source);

        KernelEvents
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .Value
            .Should()
            .Be(5);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task it_can_reference_system_text_json(Language language)
    {
        var source = language switch
        {
            Language.FSharp => new[]
            {
                @"open System.Text.Json;
let jsonException = JsonException()
let location = jsonException.GetType().Assembly.Location
location.EndsWith(""System.Text.Json.dll"")"
            },

            Language.CSharp => new[]
            {
                @"using System.Text.Json;
var jsonException = new JsonException();
var location = jsonException.GetType().Assembly.Location;
location.EndsWith(""System.Text.Json.dll"")"
            }
        };

        var kernel = CreateKernel(language);

        await SubmitCode(kernel, source);

        KernelEvents
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .Value
            .Should()
            .Be(true);
    }

    [Theory]
    [InlineData(Language.FSharp)]
    public async Task kernel_base_ignores_command_line_directives(Language language)
    {
        // The text `[1;2;3;4]` parses as a System.CommandLine directive; ensure it's not consumed and is passed on to the kernel.
        var kernel = CreateKernel(language);

        var source = @"
[1;2;3;4]
|> List.sum";

        await SubmitCode(kernel, source);
        var events = KernelEvents;

        events
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .Value
            .Should()
            .Be(10);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task when_it_throws_exception_after_a_value_was_produced_then_only_the_error_is_returned(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.FSharp => new[]
            {
                "open System",
                "2 + 2",
                "adddddddddd"
            },

            Language.CSharp => new[]
            {
                "using System;",
                "2 + 2",
                "adddddddddd"
            }
        };

        await SubmitCode(kernel, source);

        var positionalEvents = KernelEvents
            .Select((e, pos) => (e, pos)).ToList();

        var (failure, lastFailureIndex) = positionalEvents
            .Single(p => p.e is CommandFailed);

        ((CommandFailed)failure).Exception.Should().BeOfType<CodeSubmissionCompilationErrorException>();

        var lastCodeSubmissionPosition = positionalEvents
            .Last(p => p.e is CodeSubmissionReceived).pos;

        var lastValueProducedPosition = positionalEvents
            .Last(p => p.e is ReturnValueProduced).pos;

        lastValueProducedPosition
            .Should()
            .BeLessThan(lastCodeSubmissionPosition);
        lastCodeSubmissionPosition
            .Should()
            .BeLessThan(lastFailureIndex);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task it_returns_exceptions_thrown_in_user_code(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.FSharp => new[]
            {
                // F# syntax doesn't allow a bare `raise ...` expression at the root due to type inference being
                // ambiguous, but the same effect can be achieved by wrapping the exception in a strongly-typed
                // function call.
                "open System",
                "let f (): unit = raise (new DataMisalignedException())",
                "f ()"
            },

            Language.CSharp => new[]
            {
                @"
void f()
{
    try
    {
        throw new Exception(""inner"");
    }
    catch(Exception e)
    {
        throw new DataMisalignedException(""outer"", e);
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
            .Exception
            .Should()
            .BeOfType<DataMisalignedException>();
    }

    [Theory]
    [InlineData(Language.CSharp, "CS0103", "aaaadd", "(1,1): error CS0103:")]
    [InlineData(Language.FSharp, "FS0039", "aaaadd", "input.fsx (1,1)-(1,7) typecheck error")]
    public async Task when_code_contains_compile_time_error_diagnostics_are_produced(Language language, string code, string diagnosticMessageFragment, string errorMessageFragment)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.FSharp => new[]
            {
                "open System",
                "aaaadd"
            },

            Language.CSharp => new[]
            {
                "using System;",
                "aaaadd"
            }
        };

        await SubmitCode(kernel, source);

        var diagnosticRange = new LinePositionSpan(
            new LinePosition(0, 0),
            new LinePosition(0, 6));

        using var _ = new AssertionScope();

        // The CommandFailed message is populated
        KernelEvents
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Contain(errorMessageFragment);

        // The Diagnostics of DiagnosticsProduced event are populated
        KernelEvents
            .Should()
            .ContainSingle<DiagnosticsProduced>(d => d.Diagnostics.Count > 0)
            .Which
            .Diagnostics
            .Should()
            .ContainSingle(diag =>
                diag.LinePositionSpan == diagnosticRange &&
                diag.Code == code &&
                diag.Message.Contains(diagnosticMessageFragment));

        // The FormattedValues are populated of DiagnosticsProduced event are populated
        KernelEvents
            .Should()
            .ContainSingle<DiagnosticsProduced>(d => d.Diagnostics.Count > 0)
            .Which
            .FormattedDiagnostics
            .Should()
            .ContainSingle(fv => true)
            .Which
            .Value
            .Should()
            .Contain(errorMessageFragment);

    }

    [Theory]
    [InlineData(Language.CSharp, "AppDomain.GetCurrentThreadId()", "'AppDomain.GetCurrentThreadId has been deprecated")]
    [InlineData(Language.FSharp, "AppDomain.GetCurrentThreadId has been deprecated")]
    public async Task when_code_contains_compile_time_warnings_diagnostics_are_produced(Language language, params string[] diagnosticMessageFragments)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.FSharp => new[]
            {
                "System.AppDomain.GetCurrentThreadId()"
            },

            Language.CSharp => new[]
            {
                "System.AppDomain.GetCurrentThreadId()",
            }
        };

        await SubmitCode(kernel, source);

        using var _ = new AssertionScope();

        KernelEvents
            .Should()
            .ContainSingle<ReturnValueProduced>();

        KernelEvents
            .Should()
            .ContainSingle<DiagnosticsProduced>(d => d.Diagnostics.Count > 0)
            .Which
            .Diagnostics
            .Should()
            .ContainSingle(diagnostic => true)
            .Which
            .Message
            .Should()
            .ContainAll(diagnosticMessageFragments);
    }

    [Fact]
    public async Task powershell_produces_diagnostics_from_parse_errors()
    {
        var kernel = CreateKernel(Language.PowerShell);

        await kernel.SubmitCodeAsync("::()");

        var diagnosticRange = new LinePositionSpan(
            new LinePosition(0, 4),
            new LinePosition(0, 4));

        KernelEvents
            .Should()
            .ContainSingle<DiagnosticsProduced>(d => d.Diagnostics.Count > 0)
            .Which
            .Diagnostics
            .Should()
            .ContainSingle(d =>
                d.LinePositionSpan == diagnosticRange &&
                d.Code == "ExpectedExpression" &&
                d.Message == "An expression was expected after '('.");

        KernelEvents
            .Should()
            .ContainSingle<DiagnosticsProduced>(d => d.Diagnostics.Count > 0)
            .Which
            .FormattedDiagnostics
            .Should()
            .ContainSingle(fv => true)
            .Which
            .Value
            .Should()
            .StartWith("At line:1 char:4");
    }

    [Theory]
    [InlineData(Language.CSharp, @"
int SomeFunction(int n)
{
    return n switch
    {
        0 => 0
    };
}

", "CS8509", "switch", "1")]
    [InlineData(Language.FSharp, @"
let x n =
    match n with
    | 0 -> ()
", "FS0025", "1")]
    public async Task diagnostics_are_produced_on_command_succeeded(Language language, string code, string errorCode, params string[] diagnosticMessageFragments)
    {
        var kernel = CreateKernel(language);

        await kernel.SubmitCodeAsync(code);

        using var _ = new AssertionScope();

        KernelEvents
            .Should()
            .ContainSingle<CommandSucceeded>();

        KernelEvents
            .Should()
            .ContainSingle<DiagnosticsProduced>(d => d.Diagnostics.Count > 0)
            .Which
            .Diagnostics
            .Should()
            .ContainSingle(diag =>
                diag.Code == errorCode &&
                diagnosticMessageFragments.All(
                    frag => diag.Message.Contains(frag)
                ));
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task shadowing_variable_does_not_produce_diagnostics(Language language)
    {
        var kernel = CreateKernel(language);

        var firstDeclaration = language switch
        {
            // null returned.
            Language.FSharp => "let a = \"original\"",
            Language.CSharp => "var a = \"original\";"
        };

        await SubmitCode(kernel, firstDeclaration);

        var shadowingDeclaration = language switch
        {
            // null returned.
            Language.FSharp => "let a = 1",
            Language.CSharp => "var a = 1;"
        };

        await SubmitCode(kernel, shadowingDeclaration);

        KernelEvents
            .OfType<DiagnosticsProduced>()
            .SelectMany(dp => dp.Diagnostics)
            .Should()
            .BeEmpty();
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task accessing_shadowed_variable_does_not_produce_diagnostics(Language language)
    {
        var kernel = CreateKernel(language);

        var firstDeclaration = language switch
        {
            // null returned.
            Language.FSharp => "let a = \"original\"",
            Language.CSharp => "var a = \"original\";"
        };

        await SubmitCode(kernel, firstDeclaration);

        var shadowingDeclaration = language switch
        {
            // null returned.
            Language.FSharp => "let a = 1",
            Language.CSharp => "var a = 1;"
        };

        await SubmitCode(kernel, shadowingDeclaration);

        await SubmitCode(kernel, "a");

        KernelEvents
            .OfType<DiagnosticsProduced>()
            .SelectMany(dp => dp.Diagnostics)
            .Should()
            .BeEmpty();
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task typing_shadowed_variable_does_not_produce_diagnostics(Language language)
    {
        var kernel = CreateKernel(language);

        var firstDeclaration = language switch
        {
            // null returned.
            Language.FSharp => "let a = \"original\"",
            Language.CSharp => "var a = \"original\";"
        };

        await SubmitCode(kernel, firstDeclaration);

        var shadowingDeclaration = language switch
        {
            // null returned.
            Language.FSharp => "let a = 1",
            Language.CSharp => "var a = 1;"
        };

        await SubmitCode(kernel, shadowingDeclaration);

        await kernel.SendAsync(new RequestDiagnostics("a"));

        KernelEvents
            .OfType<DiagnosticsProduced>()
            .SelectMany(dp => dp.Diagnostics)
            .Should()
            .BeEmpty();
    }

    [Theory]
    [InlineData(Language.CSharp, "Console.WritLin();")]
    [InlineData(Language.FSharp, "printfnnn \"\"")]
    [InlineData(Language.PowerShell, "::()")]
    public async Task produced_diagnostics_are_remapped_to_the_appropriate_span(Language language, string languageSpecificCode)
    {
        var kernel = CreateKernel(language);

        var fullCode = $@"

#!time

$${languageSpecificCode}
";

        MarkupTestFile.GetLineAndColumn(fullCode, out var code, out var line, out var _column);

        await SubmitCode(kernel, code);

        KernelEvents
            .Should()
            .ContainSingle<DiagnosticsProduced>(d => d.Diagnostics.Count > 0)
            .Which
            .Diagnostics
            .Should()
            .ContainSingle()
            .Which
            .LinePositionSpan.Start.Line
            .Should()
            .Be(line);
    }

    [Theory]
    [InlineData(Language.CSharp, "Console.WriteLineeeeeee();", "CS0117")]
    [InlineData(Language.FSharp, "printfnnnnnn \"\"", "FS0039")]
    [InlineData(Language.PowerShell, "::()", "ExpectedExpression")]
    public async Task diagnostics_can_be_directly_requested(Language language, string source, string diagnosticCode)
    {
        var kernel = CreateKernel(language);

        await kernel.SendAsync(new RequestDiagnostics(source));

        KernelEvents
            .Should()
            .ContainSingle<DiagnosticsProduced>(d => d.Diagnostics.Count == 1)
            .Which
            .Diagnostics
            .Should()
            .ContainSingle(diag => diag.Code == diagnosticCode);
    }

    [Theory]
    [InlineData(Language.CSharp, "Console.WriteLineeeeeee();")]
    [InlineData(Language.FSharp, "printfnnnnnn \"\"")]
    [InlineData(Language.PowerShell, "::()")]
    public async Task requested_diagnostics_are_remapped_to_the_appropriate_span(Language language, string languageSpecificCode)
    {
        var kernel = CreateKernel(language);

        var fullCode = $@"

#!time

$${languageSpecificCode}
";

        MarkupTestFile.GetLineAndColumn(fullCode, out var code, out var line, out var _column);

        await kernel.SendAsync(new RequestDiagnostics(code));

        KernelEvents
            .Should()
            .ContainSingle<DiagnosticsProduced>(d => d.Diagnostics.Count == 1)
            .Which
            .Diagnostics
            .Should()
            .ContainSingle(diag => diag.LinePositionSpan.Start.Line == line);
    }

    [Theory]
    [InlineData(Language.FSharp)]
    [InlineData(Language.CSharp)]
    public async Task RequestCompletions_prevents_RequestDiagnostics_from_producing_events(Language language)
    {
        using var kernel = CreateKernel(language);

        MarkupTestFile.GetLineAndColumn("Console.$$", out var output, out var line, out var column);

        var requestDiagnosticsCommand = new RequestDiagnostics(output);

        var requestCompletionsCommand = new RequestCompletions(output, new LinePosition(line, column));

        var diagnosticsResultTask = kernel.SendAsync(requestDiagnosticsCommand);
        await kernel.SendAsync(requestCompletionsCommand);

        var diagnosticsResult = await diagnosticsResultTask;

        diagnosticsResult.Events.Should().NotContain(e => e is DiagnosticsProduced);
    }

    [Theory]
    [InlineData(Language.CSharp, "Console.WriteLineeeeeee();")]
    [InlineData(Language.FSharp, "printfnnnnnn \"\"")]
    [InlineData(Language.PowerShell, "::()")]
    public async Task requested_diagnostics_does_not_execute_directives_handlers(Language language, string languageSpecificCode)
    {
        var kernel = CreateKernel(language);
        var handlerInvoked = false;
        kernel.AddDirective(new KernelActionDirective("#!custom"), (command, context) =>
            {
                handlerInvoked = true;
                return Task.CompletedTask;
            });

        var fullCode = $"""
                        #!time
                        #!custom
                        $${languageSpecificCode}

                        """;

        MarkupTestFile.GetLineAndColumn(fullCode, out var code, out var line, out var _column);

        await kernel.SendAsync(new RequestDiagnostics(code));

        handlerInvoked
            .Should()
            .BeFalse();
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.PowerShell)]
    // no F# equivalent, because it doesn't have the concept of complete/incomplete submissions
    public async Task it_acknowledges_receipt_of_incomplete_submissions(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.CSharp => "var a =",
            Language.PowerShell => "$a ="
        };

        await SubmitCode(kernel, source);

        KernelEvents
            .Single(e => e is IncompleteCodeSubmissionReceived);

        KernelEvents
            .Should()
            .Contain(e => e is IncompleteCodeSubmissionReceived);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.PowerShell)]
    // no F# equivalent, because it doesn't have the concept of complete/incomplete submissions
    public async Task it_acknowledged_receipt_of_complete_submissions_having_return_value(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.CSharp => "25",
            Language.PowerShell => "25",
        };

        await SubmitCode(kernel, source);
        
        KernelEvents
            .Should()
            .Contain(e => e is CompleteCodeSubmissionReceived);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    // no F# equivalent, because it doesn't have the concept of complete/incomplete submissions
    public async Task it_acknowledged_receipt_of_complete_stdio_submissions(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.CSharp => "Console.WriteLine(\"Hello\");"
        };

        await SubmitCode(kernel, source);

        KernelEvents
            .Should()
            .Contain(e => e is CompleteCodeSubmissionReceived);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task expression_evaluated_to_null_has_result_with_null_value(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            // null returned.
            Language.FSharp => "null :> obj",
            Language.CSharp => "null as object"
        };

        await SubmitCode(kernel, source);

        KernelEvents
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .Value
            .Should()
            .BeNull();
    }

    [Theory]
    [InlineData(Language.CSharp)]
    // F# doesn't have the concept of a statement
    public async Task it_does_not_return_a_result_for_a_statement(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            // if is a statement in C#
            Language.CSharp => "if (true) { }"
        };

        await SubmitCode(kernel, source);

        KernelEvents
            .Should()
            .NotContain(e => e is StandardOutputValueProduced);

        KernelEvents
            .Should()
            .NotContain(e => e is ReturnValueProduced);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task it_does_not_return_a_result_for_a_binding(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.FSharp => "let x = 1",
            Language.CSharp => "var x = 1;"
        };

        await SubmitCode(kernel, source);

        KernelEvents
            .Should()
            .NotContain(e => e is StandardOutputValueProduced);

        KernelEvents
            .Should()
            .NotContain(e => e is ReturnValueProduced);
    }

    [Theory]
    [InlineData(Language.CSharp, "true ? 25 : 20")]
    [InlineData(Language.FSharp, "if true then 25 else 20")]
    [InlineData(Language.FSharp, "if false then 15 elif true then 25 else 20")]
    [InlineData(Language.CSharp, "true switch { true => 25, false => 20 }")]
    [InlineData(Language.FSharp, "match true with | true -> 25; | false -> 20")]
    public async Task it_returns_a_result_for_a_if_expressions(Language language, string expression)
    {
        var kernel = CreateKernel(language);

        await SubmitCode(kernel, expression);

        KernelEvents.OfType<ReturnValueProduced>()
            .Last()
            .Value
            .Should()
            .Be(25);
    }

    [Theory]
    [InlineData(Language.CSharp, """
                                 using Microsoft.DotNet.Interactive; 
                                 FormattedValue.CreateSingleFromObject(1)
                                 """)]
    [InlineData(Language.FSharp, """
                                 open Microsoft.DotNet.Interactive
                                 FormattedValue.CreateSingleFromObject(1)
                                 """)]
                                
    [InlineData(Language.CSharp, """
                                 using Microsoft.DotNet.Interactive; 
                                 FormattedValue.CreateManyFromObject(1, "text/plain","application/json")
                                 """)]
    [InlineData(Language.FSharp, """
                                 open Microsoft.DotNet.Interactive
                                 FormattedValue.CreateManyFromObject(1, "text/plain","application/json")
                                 """)]
                                 
    public async Task it_returns_formattedValue_without_additional_formatting(Language language, string expression)
    {
        var kernel = CreateKernel(language);

        await SubmitCode(kernel, expression);

        var returnValueProduced = KernelEvents.Should().ContainSingle<ReturnValueProduced>().Which;

        var returnedValues = returnValueProduced.Value switch
        {
            IEnumerable<FormattedValue> formattedValues => formattedValues,
            FormattedValue formattedValue => new[] { formattedValue },
            _ => throw new InvalidOperationException()
        };
        returnValueProduced.FormattedValues.Should().BeEquivalentTo(returnedValues);
    }


    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task it_aggregates_multiple_submissions(Language language)
    {   
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.FSharp => new[]
            {
                // Todo: decide what to do with F# not auto-opening System.Collections.Generic, System.Linq
                "open System.Collections.Generic",
                "open System.Linq",
                "let x = List<int>([|1;2|])",
                "x.Add(3)",
                "x.Max()"
            },

            Language.CSharp => new[]
            {
                "var x = new List<int>{1,2};",
                "x.Add(3);",
                "x.Max()"
            }
        };

        await SubmitCode(kernel, source);

        KernelEvents.OfType<ReturnValueProduced>()
            .Last()
            .Value
            .Should()
            .Be(3);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task it_produces_values_when_executing_Console_output(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.FSharp => """

                               open System
                               Console.Write("value one")
                               Console.Write("value two")
                               Console.Write("value three")
                               """,

            Language.CSharp => """

                               Console.Write("value one");
                               Console.Write("value two");
                               Console.Write("value three");
                               """
        };

        var result = await kernel.SendAsync(new SubmitCode(source));

        result.Events
              .OfType<StandardOutputValueProduced>()
              .Select(e => e.FormattedValues.ToArray())
              .Should()
              .BeEquivalentSequenceTo(
                  new[] { new FormattedValue("text/plain", "value one") },
                  new[] { new FormattedValue("text/plain", "value two") },
                  new[] { new FormattedValue("text/plain", "value three") });
    }

    [Theory]
    [InlineData(Language.FSharp)]
    public async Task kernel_captures_stdout(Language language)
    {
        var kernel = CreateKernel(language);

        var source = "printf \"hello from F#\"";

        var result = await SubmitCode(kernel, source);

        result.Events
              .OfType<StandardOutputValueProduced>()
              .Last()
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType &&
                                  v.Value == "hello from F#");
    }

    [Theory]
    [InlineData(Language.FSharp)]
    public async Task kernel_captures_stderr(Language language)
    {
        var kernel = CreateKernel(language);

        var source = "eprintf \"hello from F#\"";

        await SubmitCode(kernel, source);

        KernelEvents
            .OfType<StandardErrorValueProduced>()
            .Last()
            .FormattedValues
            .Should()
            .ContainSingle(v => v.Value == "hello from F#");
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task it_returns_a_similarly_shaped_error(Language language)
    {
        var kernel = CreateKernel(language, openTestingNamespaces: true);

        var (source, errorFragments) = language switch
        {
            Language.CSharp => ("using Not.A.Namespace;", new[] { "(1,7): error CS0246:", "Not", "using" }),
            Language.FSharp => ("open Not.A.Namespace", new[] { @"input.fsx (1,6)-(1,9) typecheck error", "Not" })
        };

        await SubmitCode(kernel, source);

        KernelEvents
            .Should()
            .ContainSingle<DiagnosticsProduced>(d => d.Diagnostics.Count > 0)
            .Which
            .FormattedDiagnostics
            .Should()
            .ContainSingle(fv => true)
            .Which
            .Value
            .Should()
            .ContainAll(errorFragments);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task it_produces_a_final_value_if_the_code_expression_evaluates(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.FSharp => @"
open System
""value one"".Display()
""value two"".Display()
""value three"".Display()
5",

            Language.CSharp => @"
""value one"".Display();
""value two"".Display();
""value three"".Display();
5",
        };

        var result = await SubmitCode(kernel, source);

        result.Events
              .OfType<DisplayedValueProduced>()
              .Should()
              .HaveCount(3);

        result.Events
              .OfType<ReturnValueProduced>()
              .Last()
              .Value.Should().Be(5);

    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task the_output_is_asynchronous(Language language)
    {
        var kernel = CreateKernel(language);
        var timeStampedEvents = kernel.KernelEvents.Timestamp().ToSubscribedList();

        var source = language switch
        {
            Language.FSharp => @"
open System
1s.Display(""text/plain"")
System.Threading.Thread.Sleep(1000)
2s.Display(""text/plain"")
5",

            Language.CSharp => @"
1.Display(""text/plain"");
System.Threading.Thread.Sleep(1000);
2.Display(""text/plain"");
5",
        };

        await SubmitCode(kernel, source);

        var events =
            timeStampedEvents
                .Where(e => e.Value is DisplayedValueProduced)
                .ToArray();

        events.Should().HaveCount(2);

        var diff = events[1].Timestamp - events[0].Timestamp;

        diff.Should().BeCloseTo(1.Seconds(), precision: 0.5.Seconds());
        events
            .Select(e => e.Value as DisplayedValueProduced)
            .SelectMany(e => e.FormattedValues.Select(v => v.Value))
            .Should()
            .BeEquivalentTo(new[] { "1", "2" });

    }

    [Fact]
    public async Task it_supports_csharp_8()
    {
        var kernel = CreateKernel();

        await kernel.SendAsync(new SubmitCode("var text = \"meow? meow!\";"));
        await kernel.SendAsync(new SubmitCode("text[^5..^0]"));

        KernelEvents
            .OfType<ReturnValueProduced>()
            .Last()
            .Value
            .Should()
            .Be("meow!");
    }

    [Fact]
    public async Task it_supports_csharp_11()
    {
        using var kernel = CreateKernel();

        var result = await kernel.SendAsync(new SubmitCode(
                                                """
                                                public static int CheckSwitch(int[] values)
                                                    => values switch
                                                    {
                                                        [1, 2, .., 10] => 1,
                                                        [1, 2] => 2,
                                                        [1, _] => 3,
                                                        [1, ..] => 4,
                                                        [..] => 50
                                                    };
                                                """));

        result.Events.Should().ContainSingle<DiagnosticsProduced>()
              .Which.Diagnostics.Should().BeEmpty();
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task it_formats_func_instances(Language language)
    {
        var kernel = CreateKernel(language);

        var source = language switch
        {
            Language.CSharp => new[] {
                "Func<int> func = () => 1;",
                "func()",
                "func"
            },

            Language.FSharp => new[] {
                "let func () = 1",
                "func()",
                "func"
            }
        };

        await SubmitCode(kernel, source);

        KernelEvents
            .OfType<ReturnValueProduced>()
            .Should()
            .Contain(e => ((SubmitCode)e.Command).Code == source[1])
            .And
            .Contain(e => ((SubmitCode)e.Command).Code == source[2]);
    }

    [Theory]
    [InlineData(Language.CSharp, "System.", "IO")]
    [InlineData(Language.FSharp, "System.", "IO")]
    [InlineData(Language.PowerShell, "[System.", "System.IO")]
    // Also tests index is calculated properly.
    [InlineData(Language.PowerShell, "$a = [System.", "System.IO")]
    public async Task it_returns_completion_list_for_types(Language language, string codeToComplete, string expectedCompletion)
    {
        var kernel = CreateKernel(language);
        await kernel.SendAsync(new RequestCompletions(codeToComplete, new LinePosition(0, codeToComplete.Length)));

        KernelEvents
            .OfType<CompletionsProduced>()
            .Single()
            .Completions
            .Should()
            .Contain(i => i.DisplayText == expectedCompletion);
    }

    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    [InlineData(Language.PowerShell)]
    public async Task it_returns_completion_list_for_previously_declared_items(Language language)
    {
        var kernel = CreateKernel(language, openTestingNamespaces: true);

        var source = language switch
        {
            Language.FSharp => @"let alpha = new Random()",
            Language.CSharp => @"var alpha = new Random();",
            Language.PowerShell => @"function alpha { 5 }",
        };

        await SubmitCode(kernel, source);

        await kernel.SendAsync(new RequestCompletions("al", new LinePosition(0, 2)));

        KernelEvents
            .OfType<CompletionsProduced>()
            .Single()
            .Completions
            .Should()
            .Contain(i => i.DisplayText == "alpha");
    }

    [Fact]
    public async Task When_submission_is_split_then_CommandHandled_is_published_only_for_the_root_command()
    {
        var kernel = CreateKernel(Language.CSharp);

        var command = new SubmitCode("#!whos \nvar x = 1;");

        await kernel.SendAsync(command);

        KernelEvents
            .Should()
            .ContainSingle<CommandSucceeded>(c => c.Command == command)
            .Which
            .Command
            .Should()
            .Be(command);
    }

    [Fact]
    public async Task Racing_commands_on_fast_track_and_main_track_does_not_throw()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel(),
            new FSharpKernel()
        };

        var fast1 = kernel.SendAsync(new RequestKernelInfo("csharp"));
        var fast2 = kernel.SendAsync(new RequestKernelInfo("fsharp"));
        var fast3 = kernel.SendAsync(new RequestDiagnostics("var x = 123;", "csharp"));
        var main1 = await kernel.SendAsync(new SubmitCode("123", "csharp"));

        foreach (var dp in main1.Events.OfType<DiagnosticsProduced>())
        {
            dp.Diagnostics.Should().BeEmpty();
        }

        (await fast1).Events.Should().NotContainErrors();
        (await fast2).Events.Should().NotContainErrors();
        (await fast3).Events.Should().NotContainErrors();
        main1.Events.Should().NotContainErrors();
    }

    [Fact]
    public void Parallel_calls_to_IdleAsync_do_not_throw()
    {
        using var scheduler = new KernelScheduler<int, int>();
        using var startBarrier = new Barrier(3);
        using var doneBarrier = new Barrier(4);
        Exception exception = null;

        ThreadStart idleAsync = () =>
        {
            startBarrier.SignalAndWait();

            try
            {
                 scheduler.IdleAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }

            doneBarrier.SignalAndWait();
        };

        foreach (var _ in Enumerable.Range(1, 3))
        {
            new Thread(idleAsync).Start();
        }

        doneBarrier.SignalAndWait();

        exception.Should().BeNull();
    }
}