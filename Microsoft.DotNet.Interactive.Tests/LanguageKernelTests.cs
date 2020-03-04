// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Management.Automation;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using XPlot.Plotly;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable 8509
namespace Microsoft.DotNet.Interactive.Tests
{
    public class LanguageKernelTests : LanguageKernelTestBase
    {
        public LanguageKernelTests(ITestOutputHelper output) : base(output)
        {
        }

        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
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
        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp, Skip = "Issue #695 - dotnet-interactive with an F# notebook does not load System.Text.Json")]
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

        [Theory(Timeout = 45000)]
        [InlineData(Language.FSharp)]
        public async Task kernel_base_ignores_command_line_directives(Language language)
        {
            // The text `[1;2;3;4]` parses as a System.CommandLine directive; ensure it's not consumed and is passed on to the kernel.
            var kernel = CreateKernel(language);

            var source = @"
[1;2;3;4]
|> List.sum";

            await SubmitCode(kernel, source);

            KernelEvents
                .OfType<ReturnValueProduced>()
                .Last()
                .Value
                .Should()
                .Be(10);
        }

        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_returns_diagnostics(Language language)
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

            var error = language switch
            {
                Language.FSharp => "input.fsx (1,1)-(1,7) typecheck error The value or constructor 'aaaadd' is not defined.",
                Language.CSharp => "(1,1): error CS0103: The name 'aaaadd' does not exist in the current context"
            };

            KernelEvents
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
                .Should()
                .Be(error);
        }

        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
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


        [Theory(Timeout = 45000)]
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

        [Theory(Timeout = 45000)]
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
                    new StandardOutputValueProduced("value one", kernelCommand, new[] { new FormattedValue("text/plain", "value one") }),
                    new StandardOutputValueProduced("value two", kernelCommand, new[] { new FormattedValue("text/plain", "value two") }),
                    new StandardOutputValueProduced("value three", kernelCommand, new[] { new FormattedValue("text/plain", "value three") }));
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.FSharp)]
        public async Task kernel_captures_stdout(Language language)
        {
            var kernel = CreateKernel(language);

            var source = "printf \"hello from F#\"";

            await SubmitCode(kernel, source);

            KernelEvents
                .OfType<StandardOutputValueProduced>()
                .Last()
                .Value
                .Should()
                .Be("hello from F#");
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.FSharp)]
        public async Task kernel_captures_stderr(Language language)
        {
            var kernel = CreateKernel(language);

            var source = "eprintf \"hello from F#\"";

            await SubmitCode(kernel, source);

            KernelEvents
                .OfType<StandardErrorValueProduced>()
                .Last()
                .Value
                .Should()
                .Be("hello from F#");
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_returns_a_similarly_shaped_error(Language language)
        {
            var kernel = CreateKernel(language);

            var (source, error) = language switch
            {
                Language.CSharp => ("using Not.A.Namespace;", "(1,7): error CS0246: The type or namespace name 'Not' could not be found (are you missing a using directive or an assembly reference?)"),
                Language.FSharp => ("open Not.A.Namespace", @"input.fsx (1,6)-(1,9) typecheck error The namespace or module 'Not' is not defined. Maybe you want one of the following:
   Net")
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .Should()
                .ContainSingle<CommandFailed>()
                .Which
                .Message
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

        [Theory(Timeout = 45000)]
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
            events.Select(e => ((StandardOutputValueProduced) e.Value).Value)
                .Should()
                .BeEquivalentTo(new [] {"1", "2"});

        }

        [Fact(Skip = "requires support for cs8 in roslyn scripting")]
        public async Task it_supports_csharp_8()
        {
            var kernel = CreateKernel();

            await kernel.SendAsync(new SubmitCode("var text = \"meow? meow!\";"));
            await kernel.SendAsync(new SubmitCode("text[^5..^0]"));

            KernelEvents
                .OfType<StandardOutputValueProduced>()
                .Last()
                .Value
                .Should()
                .Be("meow!");
        }



        [Theory(Timeout = 45000)]
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
                },
            };

            await SubmitCode(kernel, source);

            KernelEvents
                .OfType<ReturnValueProduced>()
                .Should()
                .Contain(e => ((SubmitCode)e.Command).Code == source[1])
                .And
                .Contain(e => ((SubmitCode)e.Command).Code == source[2]);
        }


        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task it_returns_completion_list_for_types(Language language)
        {
            var kernel = CreateKernel(language);

            var source = "System.Console."; // same code is valid regardless of the language

            await kernel.SendAsync(new RequestCompletion(source, 15));

            KernelEvents
                .Should()
                .ContainSingle(e => e is CompletionRequestReceived);

            KernelEvents
                .OfType<CompletionRequestCompleted>()
                .Single()
                .CompletionList
                .Should()
                .Contain(i => i.DisplayText == "ReadLine");
        }

        [Theory(Timeout = 45000)]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        [InlineData(Language.PowerShell)]
        public async Task it_returns_completion_list_for_previously_declared_items(Language language)
        {
            var kernel = CreateKernel(language);

            var source = language switch
            {
                Language.FSharp => @"let alpha = new Random()",
                Language.CSharp => @"var alpha = new Random();",
                Language.PowerShell => @"function alpha { 5 }",
            };

            await SubmitCode(kernel, source);

            await kernel.SendAsync(new RequestCompletion("al", 2));

            KernelEvents
                        .Should()
                        .ContainSingle(e => e is CompletionRequestReceived);

            KernelEvents
                        .OfType<CompletionRequestCompleted>()
                        .Single()
                        .CompletionList
                        .Should()
                        .Contain(i => i.DisplayText == "alpha");
        }

        [Fact(Timeout = 45000)]
        public async Task Script_state_is_available_within_middleware_pipeline()
        {
            var variableCountBeforeEvaluation = 0;
            var variableCountAfterEvaluation = 0;

            using var kernel = new CSharpKernel();

            kernel.AddMiddleware(async (command, context, next) =>
            {
                var k = context.HandlingKernel as CSharpKernel;

                await next(command, context);

                variableCountAfterEvaluation = k.ScriptState.Variables.Length;
            });

            await kernel.SendAsync(new SubmitCode("var x = 1;"));

            variableCountBeforeEvaluation.Should().Be(0);
            variableCountAfterEvaluation.Should().Be(1);
        }

        [Fact(Timeout = 45000)]
        public async Task When_submission_is_split_then_CommandHandled_is_published_only_for_the_root_command()
        {
            var kernel = CreateKernel(Language.CSharp);

            var command = new SubmitCode("#!whos \nvar x = 1;");

            await kernel.SendAsync(command);

            KernelEvents
                .Should()
                .ContainSingle<CommandHandled>(c => c.Command == command)
                .Which
                .Command
                .Should()
                .Be(command);
        }

        [Fact()]
        public async Task PowerShell_streams_handled_in_correct_order()
        {
            var kernel = CreateKernel(Language.PowerShell);

            const string yellow_foreground = "\u001b[93m";
            const string red_foreground = "\u001b[91m";
            const string reset = "\u001b[0m";

            const string warningMessage = "I am a warning message";
            const string verboseMessage = "I am a verbose message";
            const string outputMessage = "I am output";
            const string debugMessage = "I am a debug message";
            const string hostMessage = "I am a message written to host";
            const string errorMessage = "I am a non-terminating error";

            var command = new SubmitCode($@"
Write-Warning '{warningMessage}'
Write-Verbose '{verboseMessage}' -Verbose
'{outputMessage}'
Write-Debug '{debugMessage}' -Debug
Write-Host '{hostMessage}' -NoNewline
Write-Error '{errorMessage}'
");

            await kernel.SendAsync(command);

            Assert.Collection(KernelEvents,
                e => e.Should().BeOfType<CodeSubmissionReceived>(),
                e => e.Should().BeOfType<CompleteCodeSubmissionReceived>(),
                e => e.Should().BeOfType<StandardOutputValueProduced>().Which
                    .Value.ToString().Should().Contain($"{yellow_foreground}WARNING: {warningMessage}{reset}"),
                e => e.Should().BeOfType<StandardOutputValueProduced>().Which
                    .Value.ToString().Should().Contain($"{yellow_foreground}VERBOSE: {verboseMessage}{reset}"),
                e => e.Should().BeOfType<StandardOutputValueProduced>().Which
                    .Value.ToString().Should().Be(outputMessage + Environment.NewLine),
                e => e.Should().BeOfType<StandardOutputValueProduced>().Which
                    .Value.ToString().Should().Contain($"{yellow_foreground}DEBUG: {debugMessage}{reset}"),
                e => e.Should().BeOfType<StandardOutputValueProduced>().Which
                    .Value.ToString().Should().Be(hostMessage),
                e => e.Should().BeOfType<StandardOutputValueProduced>().Which
                    .Value.ToString().Should().Contain($"{red_foreground}Write-Error: {red_foreground}{errorMessage}{reset}"),
                e => e.Should().BeOfType<CommandHandled>());
        }

        [Fact()]
        public async Task PowerShell_progress_sends_updated_display_values()
        {
            var kernel = CreateKernel(Language.PowerShell);
            var command = new SubmitCode(@"
for ($j = 0; $j -le 4; $j += 4 ) {
    $p = $j * 25
    Write-Progress -Id 1 -Activity 'Search in Progress' -Status ""$p% Complete"" -PercentComplete $p
    Start-Sleep -Milliseconds 300
}
");
            await kernel.SendAsync(command);

            Assert.Collection(KernelEvents,
                e => e.Should().BeOfType<CodeSubmissionReceived>(),
                e => e.Should().BeOfType<CompleteCodeSubmissionReceived>(),
                e => e.Should().BeOfType<DisplayedValueProduced>().Which
                    .Value.Should().BeOfType<string>().Which
                    .Should().Match("* Search in Progress* 0% Complete* [ * ] *"),
                e => e.Should().BeOfType<DisplayedValueUpdated>().Which
                    .Value.Should().BeOfType<string>().Which
                    .Should().Match("* Search in Progress* 100% Complete* [ooo*ooo] *"),
                e => e.Should().BeOfType<DisplayedValueUpdated>().Which
                    .Value.Should().BeOfType<string>().Which
                    .Should().Be(string.Empty),
                e => e.Should().BeOfType<CommandHandled>());
        }

        [Fact()]
        public void PowerShell_type_accelerators_present()
        {
            var kernel = CreateKernel(Language.PowerShell);

            var accelerator = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
            dynamic typeAccelerators = accelerator.GetProperty("Get").GetValue(null);
            Assert.Equal(typeAccelerators["Graph.Scatter"].FullName, $"{typeof(Graph).FullName}+Scatter");
            Assert.Equal(typeAccelerators["Layout"].FullName, $"{typeof(Layout).FullName}+Layout");
            Assert.Equal(typeAccelerators["Chart"].FullName, typeof(Chart).FullName);
        }

        [Fact()]
        public async Task PowerShell_token_variables_work()
        {
            var kernel = CreateKernel(Language.PowerShell);

            await kernel.SendAsync(new SubmitCode("echo /this/is/a/path"));
            await kernel.SendAsync(new SubmitCode("$$; $^"));

            Assert.Collection(KernelEvents,
                e => e.Should()
                        .BeOfType<CodeSubmissionReceived>()
                        .Which.Code
                        .Should().Be("echo /this/is/a/path"),
                e => e.Should()
                        .BeOfType<CompleteCodeSubmissionReceived>()
                        .Which.Code
                        .Should().Be("echo /this/is/a/path"),
                e => e.Should()
                        .BeOfType<StandardOutputValueProduced>()
                        .Which.Value.ToString()
                        .Should().Be("/this/is/a/path" + Environment.NewLine),
                e => e.Should().BeOfType<CommandHandled>(),
                e => e.Should()
                        .BeOfType<CodeSubmissionReceived>()
                        .Which.Code
                        .Should().Be("$$; $^"),
                e => e.Should()
                        .BeOfType<CompleteCodeSubmissionReceived>()
                        .Which.Code
                        .Should().Be("$$; $^"),
                e => e.Should()
                        .BeOfType<StandardOutputValueProduced>()
                        .Which.Value.ToString()
                        .Should().Be("/this/is/a/path" + Environment.NewLine),
                e => e.Should()
                        .BeOfType<StandardOutputValueProduced>()
                        .Which.Value.ToString()
                        .Should().Be("echo" + Environment.NewLine),
                e => e.Should().BeOfType<CommandHandled>());
        }

        [Fact()]
        public async Task PowerShell_get_history_should_work()
        {
            var kernel = CreateKernel(Language.PowerShell);

            await kernel.SendAsync(new SubmitCode("Get-Verb > $null"));
            await kernel.SendAsync(new SubmitCode("echo bar > $null"));
            await kernel.SendAsync(new SubmitCode("Get-History | % CommandLine"));

            var outputs = KernelEvents.OfType<StandardOutputValueProduced>();
            outputs.Should().HaveCount(2);
            Assert.Collection(outputs,
                e => e.Value.As<string>().Should().Be("Get-Verb > $null" + Environment.NewLine),
                e => e.Value.As<string>().Should().Be("echo bar > $null" + Environment.NewLine));
        }

        [Fact()]
        public async Task PowerShell_native_executable_output_is_collected()
        {
            var kernel = CreateKernel(Language.PowerShell);

            var command = Platform.IsWindows
                ? new SubmitCode("ping.exe -n 1 localhost")
                : new SubmitCode("ping -c 1 localhost");

            await kernel.SendAsync(command);

            var outputs = KernelEvents.OfType<StandardOutputValueProduced>();
            outputs.Should().HaveCountGreaterThan(1);
            outputs.First()
                .Value.ToString().ToLowerInvariant()
                .Should().Match("*ping*data*");
        }
    }
}
