// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Parsing;

public class SubmissionParserTests
{
    [Theory]
    [InlineData(Language.CSharp)]
    [InlineData(Language.FSharp)]
    public async Task Pound_i_is_dispatched_to_the_correct_kernel(Language targetKernel)
    {
        var command = new SubmitCode("#i \"nuget: SomeLocation\"", targetKernelName: targetKernel.LanguageName());

        var subCommands = await CreateSubmissionParser().SplitSubmission(command);

        subCommands
            .Should()
            .AllSatisfy(c => c.TargetKernelName.Should().Be(targetKernel.LanguageName()));
    }

    [Fact]
    public async Task DiagnosticsProduced_events_always_point_back_to_the_original_command()
    {
        using var kernel = new CSharpKernel();
        var command = new SubmitCode("#!unrecognized");
        var result = await kernel.SendAsync(command);
        result.Events.Should().ContainSingle<DiagnosticsProduced>().Which.Command.Should().BeSameAs(command);
    }

    [Fact]
    public async Task ParsedDirectives_With_Args_Consume_Newlines()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel().UseValueSharing(),
            new FSharpKernel().UseValueSharing(),
        };
        kernel.DefaultKernelName = "csharp";

        var csharpCode = @"
int x = 123;
int y = 456;";

        await kernel.SubmitCodeAsync(csharpCode);

        var fsharpCode = @"
#!share --from csharp x
#!share --from csharp y
Console.WriteLine($""{x} {y}"");";
        var commands = await kernel.SubmissionParser.SplitSubmission(new SubmitCode(fsharpCode));

        commands
            .Should()
            .HaveCount(3)
            .And
            .ContainSingle<SubmitCode>()
            .Which
            .Code
            .Should()
            .NotBeEmpty();
    }

    [Theory]
    [InlineData(@"
#r one.dll
#r two.dll", "csharp")]
    [InlineData(@"
#r one.dll
var x = 123; // with some intervening code
#r two.dll", "csharp")]
    [InlineData(@"
#r one.dll
#r two.dll", "fsharp")]
    [InlineData(@"
#r one.dll
let x = 123 // with some intervening code
#r two.dll", "fsharp")]
    public async Task Multiple_pound_r_directives_are_submitted_together(
        string code,
        string defaultKernel)
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel().UseNugetDirective(),
            new FSharpKernel().UseNugetDirective(),
        };

        kernel.DefaultKernelName = defaultKernel;

        var commands = await kernel.SubmissionParser.SplitSubmission(new SubmitCode(code));

        commands
            .Should()
            .ContainSingle<SubmitCode>()
            .Which
            .Code
            .Should()
            .ContainAll("#r one.dll", "#r two.dll");
    }

    [Fact]
    public async Task RequestDiagnostics_can_be_split_into_separate_commands()
    {
        var markupCode = @"

// before magic

#!time$$

// after magic";

        MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var _, out var _);

        var command = new RequestDiagnostics(code);
        var commands = await new CSharpKernel().UseDefaultMagicCommands().SubmissionParser.SplitSubmission(command);

        commands
            .Should()
            .ContainSingle<RequestDiagnostics>(c => c.Code.Contains("before magic") && !c.Code.Contains("after magic"));
        
        commands
            .Should()
            .ContainSingle<RequestDiagnostics>(c => c.Code.Contains("after magic") && !c.Code.Contains("before magic"));
    }

    [Fact]
    public async Task Whitespace_only_nodes_do_not_generate_separate_SubmitCode_commands()
    {
        using var kernel = new CompositeKernel
        {
            new FakeKernel("one"),
            new FakeKernel("two")
        };

        kernel.DefaultKernelName = "two";

        var commands = await kernel.SubmissionParser.SplitSubmission(
            new SubmitCode("""
                
                #!one
                
                #!two
                        
                        
                """));

        commands.Should().NotContain(c => c is SubmitCode);
    }
    
    private static SubmissionParser CreateSubmissionParser(string defaultLanguage = "csharp")
    {
        using var compositeKernel = new CompositeKernel
        {
            DefaultKernelName = defaultLanguage
        };

        compositeKernel.Add(
            new CSharpKernel()
                .UseNugetDirective()
                .UseWho(),
            new[] { "c#", "C#" });

        compositeKernel.Add(
            new FSharpKernel()
                .UseNugetDirective(),
            new[] { "f#", "F#" });

        compositeKernel.Add(
            new PowerShellKernel(),
            new[] { "powershell" });

        compositeKernel.UseDefaultMagicCommands();

        return compositeKernel.SubmissionParser;
    }
}