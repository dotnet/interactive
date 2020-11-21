// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using FluentAssertions.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable 8509
#pragma warning disable 8524
namespace Microsoft.DotNet.Interactive.Tests
{
    public sealed class LanguageKernelTests : LanguageKernelTestBase
    {
        public LanguageKernelTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory]
        [InlineData(Language.FSharp)]
        [InlineData(Language.CSharp)]
        public async Task it_returns_the_result_of_a_non_null_expression(Language language)
        {
            var kernel = CreateKernel(language);

            await SubmitCode(kernel, "123");

            KernelEvents
                .OfType<ReturnValueProduced>()
                .Last()
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
                .OfType<ReturnValueProduced>()
                .Last()
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
        [InlineData(Language.CSharp, "CS0103", "The name 'aaaadd' does not exist in the current context", "(1,1): error CS0103: The name 'aaaadd' does not exist in the current context")]
        [InlineData(Language.FSharp, "FS0039", "The value or constructor 'aaaadd' is not defined.", "input.fsx (1,1)-(1,7) typecheck error The value or constructor 'aaaadd' is not defined.")]
        public async Task when_code_contains_compile_time_error_diagnostics_are_produced(Language language, string code, string diagnosticMessage, string errorMessage)
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
                .Be(errorMessage);

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
                    diag.Message == diagnosticMessage);

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
                .Be(errorMessage);

        }

        [Theory]
        [InlineData(Language.CSharp, "'AppDomain.GetCurrentThreadId()' is obsolete: 'AppDomain.GetCurrentThreadId has been deprecated")]
        [InlineData(Language.FSharp, "This construct is deprecated. AppDomain.GetCurrentThreadId has been deprecated")]
        public async Task when_code_contains_compile_time_warnings_diagnostics_are_produced(Language language, string diagnosticMessage)
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
                .StartWith(diagnosticMessage);
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

", "The switch expression does not handle all possible values of its input type")]
        [InlineData(Language.FSharp, @"
let x n =
    match n with
    | 0 -> ()
", "Incomplete pattern matches on this expression.")]
        public async Task diagnostics_are_produced_on_command_succeeded(Language language, string code, string diagnosticText)
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
                .ContainSingle(diag => diag.Message.Contains(diagnosticText));
        }

        [Fact(Skip = "The first failed sub-command cancels all subsequent command executions; the second kernel doesn't get a chance to report.")]
        public async Task diagnostics_can_be_produced_from_multiple_subkernels()
        {
            var kernel = CreateCompositeKernel(Language.FSharp);

            var code = @"
#!fsharp
printfnnn """"

#!csharp
Console.WriteLin();
";

            await SubmitCode(kernel, code);

            KernelEvents
                .OfType<DiagnosticsProduced>()
                .SelectMany(dp => dp.Diagnostics)
                .Should()
                .ContainSingle(d => d.Code.StartsWith("CS"))
                .And
                .ContainSingle(d => d.Code.StartsWith("FS"));
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
        [InlineData(Language.CSharp, "Console.WriteLineeeeeee();")]
        [InlineData(Language.FSharp, "printfnnnnnn \"\"")]
        [InlineData(Language.PowerShell, "::()")]
        public async Task requested_diagnostics_does_not_execute_directives_handlers(Language language, string languageSpecificCode)
        {
            var kernel = CreateKernel(language);
            var handlerInvoked = false;
            kernel.AddDirective(new Command("#!custom")
            {
                Handler = CommandHandler.Create(() =>
                {
                    handlerInvoked = true;
                })
            });
            var fullCode = $@"

#!time
#!custom
$${languageSpecificCode}
";

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
        public async Task it_can_analyze_incomplete_submissions(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.CSharp => "var a =",
                Language.PowerShell => "$a ="
            };

            await SubmitCode(kernel, source, submissionType: SubmissionType.Diagnose);

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
        public async Task it_can_analyze_complete_submissions(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.CSharp => "25",
                Language.PowerShell => "25",
            };

            await SubmitCode(kernel, source, submissionType: SubmissionType.Diagnose);

            KernelEvents
                .Should()
                .NotContain(e => e is ReturnValueProduced);

            KernelEvents
                .Should()
                .Contain(e => e is CompleteCodeSubmissionReceived);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        // no F# equivalent, because it doesn't have the concept of complete/incomplete submissions
        public async Task it_can_analyze_complete_stdio_submissions(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.CSharp => "Console.WriteLine(\"Hello\")"
            };

            await SubmitCode(kernel, source, submissionType: SubmissionType.Diagnose);

            KernelEvents
                .Should()
                .NotContain(e => e is StandardOutputValueProduced);

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
                        .OfType<ReturnValueProduced>()
                        .Last()
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

            await SubmitCode(kernel, source, submissionType: SubmissionType.Run);

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
                Language.FSharp => @"
open System
Console.Write(""value one"")
Console.Write(""value two"")
Console.Write(""value three"")",

                Language.CSharp => @"
Console.Write(""value one"");
Console.Write(""value two"");
Console.Write(""value three"");"
            };

            var kernelCommand = new SubmitCode(source);

            await kernel.SendAsync(kernelCommand);

            KernelEvents
                .OfType<StandardOutputValueProduced>()
                .Should()
                .BeEquivalentTo(
                    new StandardOutputValueProduced(kernelCommand, new[] { new FormattedValue("text/plain", "value one") }),
                    new StandardOutputValueProduced(kernelCommand, new[] { new FormattedValue("text/plain", "value two") }),
                    new StandardOutputValueProduced(kernelCommand, new[] { new FormattedValue("text/plain", "value three") }));
        }

        [Theory]
        [InlineData(Language.FSharp)]
        public async Task kernel_captures_stdout(Language language)
        {
            var kernel = CreateKernel(language);

            var source = "printf \"hello from F#\"";

            await SubmitCode(kernel, source);

            KernelEvents
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

            var (source, error) = language switch
            {
                Language.CSharp => ("using Not.A.Namespace;", "(1,7): error CS0246: The type or namespace name 'Not' could not be found (are you missing a using directive or an assembly reference?)"),
                Language.FSharp => ("open Not.A.Namespace", @"input.fsx (1,6)-(1,9) typecheck error The namespace or module 'Not' is not defined. Maybe you want one of the following:
   Net")
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
                .Be(error);
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
Console.Write(""value one"")
Console.Write(""value two"")
Console.Write(""value three"")
5",

                Language.CSharp => @"
Console.Write(""value one"");
Console.Write(""value two"");
Console.Write(""value three"");
5",
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .OfType<StandardOutputValueProduced>()
                .Should()
                .HaveCount(3);

            KernelEvents
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
Console.Write(1)
System.Threading.Thread.Sleep(1000)
Console.Write(2)
5",

                Language.CSharp => @"
Console.Write(1);
System.Threading.Thread.Sleep(1000);
Console.Write(2);
5",
            };

            await SubmitCode(kernel, source);

            var events =
                timeStampedEvents
                    .Where(e => e.Value is StandardOutputValueProduced)
                    .ToArray();

            var diff = events[1].Timestamp - events[0].Timestamp;

            diff.Should().BeCloseTo(1.Seconds(), precision: 500);
            events
                .Select(e => e.Value as StandardOutputValueProduced)
                .SelectMany(e => e.FormattedValues.Select(v => v.Value))
                .Should()
                .BeEquivalentTo(new [] {"1", "2"});

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

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task TryGetVariable_returns_defined_variable(Language language)
        {
            var codeToSetVariable = language switch
            {
                Language.CSharp => "var x = 123;",
                Language.FSharp => "let x = 123",
                Language.PowerShell => "$x = 123"
            };

            var kernel = CreateKernel(language);

            await kernel.SubmitCodeAsync(codeToSetVariable);

            var languageKernel = kernel.ChildKernels.OfType<DotNetKernel>().Single();

            var succeeded = languageKernel.TryGetVariable("x", out int x);

            using var _ = new AssertionScope();

            succeeded.Should().BeTrue();
            x.Should().Be(123);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task GetVariableNames_returns_the_names_of_defined_variables(Language language)
        {
            var codeToSetVariable = language switch
            {
                Language.CSharp => "var x = 123;",
                Language.FSharp => "let x = 123",
                Language.PowerShell => "$x = 123"
            };

            var kernel = CreateKernel(language);

            await kernel.SubmitCodeAsync(codeToSetVariable);

            var languageKernel = kernel.ChildKernels.OfType<DotNetKernel>().Single();

            languageKernel.GetVariableNames().Should().Contain("x");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task SetVariableAsync_declares_the_specified_variable(Language language)
        {
            var kernel = CreateKernel(language);

            var languageKernel = kernel.ChildKernels.OfType<DotNetKernel>().Single();

            await languageKernel.SetVariableAsync("x", 123);

            var succeeded = languageKernel.TryGetVariable("x", out int x);

            using var _ = new AssertionScope();

            succeeded.Should().BeTrue();
            x.Should().Be(123);
        }
        
        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task SetVariableAsync_overwrites_an_existing_variable_of_the_same_type(Language language)
        {
            var kernel = CreateKernel(language);

            var languageKernel = kernel.ChildKernels.OfType<DotNetKernel>().Single();

            await languageKernel.SetVariableAsync("x", 123);
            await languageKernel.SetVariableAsync("x", 456);

            var succeeded = languageKernel.TryGetVariable("x", out int x);

            using var _ = new AssertionScope();

            succeeded.Should().BeTrue();
            x.Should().Be(456);
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task SetVariableAsync_can_redeclare_an_existing_variable_and_change_its_type(Language language)
        {
            var kernel = CreateKernel(language);

            var languageKernel = kernel.ChildKernels.OfType<DotNetKernel>().Single();

            await languageKernel.SetVariableAsync("x", 123);
            await languageKernel.SetVariableAsync("x", "hello");

            var succeeded = languageKernel.TryGetVariable("x", out string x);

            using var _ = new AssertionScope();

            succeeded.Should().BeTrue();
            x.Should().Be("hello");
        }
    }
}
