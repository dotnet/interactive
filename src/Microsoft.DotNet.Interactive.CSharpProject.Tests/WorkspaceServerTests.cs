// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public abstract class WorkspaceServerTests : WorkspaceServerTestsCore
{
    protected abstract Workspace CreateWorkspaceWithMainContaining(string text);

    protected WorkspaceServerTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Diagnostic_logs_do_not_show_up_in_captured_console_output()
    {
        using (LogEvents.Subscribe(e => Console.WriteLine(e.ToLogString())))
        {
            var server = GetCodeRunner();

            var result = await server.RunAsync(
                new WorkspaceRequest(
                    CreateWorkspaceWithMainContaining(
                        "Console.WriteLine(\"hi!\");"))
            );

            result.Output
                .Should()
                .BeEquivalentTo(
                    new[] { "hi!", "" },
                    options => options.WithStrictOrdering());
        }
    }

    [Fact]
    public async Task Response_indicates_when_compile_is_successful_and_signature_is_like_a_console_app()
    {
        var server = GetCodeRunner();

        var workspace = Workspace.FromSource(@"
using System;

public static class Hello
{
    public static void Main()
    {
    }
}
", workspaceType: "console");

        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        result.ShouldSucceedWithNoOutput();
    }

    [Fact]
    public async Task Response_shows_program_output_when_compile_is_successful_and_signature_is_like_a_console_app()
    {
        var output = nameof(Response_shows_program_output_when_compile_is_successful_and_signature_is_like_a_console_app);

        var server = GetCodeRunner();

        var workspace = Workspace.FromSource($@"
using System;

public static class Hello
{{
    public static void Main()
    {{
        Console.WriteLine(""{output}"");
    }}
}}", workspaceType: "console");


        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        result.ShouldSucceedWithOutput(output);
    }

    [Fact]
    public async Task Response_shows_program_output_when_compile_is_successful_and_signature_is_a_fragment_containing_console_output()
    {
        var server = GetCodeRunner();

        var request = CreateWorkspaceWithMainContaining(@"
var person = new { Name = ""Jeff"", Age = 20 };
var s = $""{person.Name} is {person.Age} year(s) old"";
Console.Write(s);");


        var result = await server.RunAsync(new WorkspaceRequest(request));

        result.ShouldSucceedWithOutput("Jeff is 20 year(s) old");
    }

    [Fact]
    public async Task When_compile_is_unsuccessful_then_no_exceptions_are_shown()
    {
        var server = GetCodeRunner();

        var request = CreateWorkspaceWithMainContaining(@"
Console.WriteLine(banana);");

        var result = await server.RunAsync(new WorkspaceRequest(request));
        result.Succeeded.Should().BeFalse();
        result.Exception.Should().BeNull();
    }

    [Fact]
    public async Task When_compile_is_unsuccessful_then_diagnostics_are_displayed_in_output()
    {
        var server = GetCodeRunner();

        var request = CreateWorkspaceWithMainContaining(@"
Console.WriteLine(banana);");

        var result = await server.RunAsync(new WorkspaceRequest(request));
        result.Succeeded.Should().BeFalse();
        result.Output
            .ShouldMatch(
                "*(2,19): error CS0103: The name \'banana\' does not exist in the current context");
    }
    
    [Fact]
    public async Task Multi_line_console_output_is_captured_correctly()
    {
        var server = GetCodeRunner();

        var request = CreateWorkspaceWithMainContaining(@"
Console.WriteLine(1);
Console.WriteLine(2);
Console.WriteLine(3);
Console.WriteLine(4);");


        var result = await server.RunAsync(new WorkspaceRequest(request));

        result.ShouldSucceedWithOutput("1", "2", "3", "4", "");
    }

    [Fact]
    public async Task Whitespace_is_preserved_in_multi_line_output()
    {
        var server = GetCodeRunner();

        var request = CreateWorkspaceWithMainContaining(@"
Console.WriteLine();
Console.WriteLine(1);
Console.WriteLine();
Console.WriteLine();
Console.WriteLine(2);");

        var result = await server.RunAsync(new WorkspaceRequest(request));

        result.ShouldSucceedWithOutput("", "1", "", "", "2", "");
    }

    [Fact(Skip = "Might be causing crashes on Linux")]
    public async Task Multi_line_console_output_is_captured_correctly_when_an_exception_is_thrown()
    {
        var server = GetCodeRunner();

        var request = CreateWorkspaceWithMainContaining($@"
Console.WriteLine(1);
Console.WriteLine(2);
throw new Exception(""oops! from {nameof(Multi_line_console_output_is_captured_correctly_when_an_exception_is_thrown)}"");
Console.WriteLine(3);
Console.WriteLine(4);");

        var result = await server.RunAsync(new WorkspaceRequest(request));

        result.ShouldSucceedWithExceptionContaining(
            $"System.Exception: oops! from {nameof(Multi_line_console_output_is_captured_correctly_when_an_exception_is_thrown)}",
            output: new[] { "1", "2" });
    }

    [Fact(Skip = "Might be causing crashes on Linux")]
    public async Task When_the_users_code_throws_on_first_line_then_it_is_returned_as_an_exception_property()
    {
        var server = GetCodeRunner();

        var request = CreateWorkspaceWithMainContaining($@"throw new Exception(""oops! from {nameof(When_the_users_code_throws_on_first_line_then_it_is_returned_as_an_exception_property)}"");");

        var result = await server.RunAsync(new WorkspaceRequest(request));

        result.ShouldSucceedWithExceptionContaining($"System.Exception: oops! from {nameof(When_the_users_code_throws_on_first_line_then_it_is_returned_as_an_exception_property)}");
    }

    [Fact(Skip = "Might be causing crashes on Linux")]
    public async Task When_the_users_code_throws_on_subsequent_line_then_it_is_returned_as_an_exception_property()
    {
        var server = GetCodeRunner();

        var request = CreateWorkspaceWithMainContaining($@"
throw new Exception(""oops! from {nameof(When_the_users_code_throws_on_subsequent_line_then_it_is_returned_as_an_exception_property)}"");");

        var result = await server.RunAsync(new WorkspaceRequest(request));

        result.ShouldSucceedWithExceptionContaining($"System.Exception: oops! from {nameof(When_the_users_code_throws_on_subsequent_line_then_it_is_returned_as_an_exception_property)}");
    }

    [Fact]
    public async Task When_a_public_void_Main_with_no_parameters_is_present_it_is_invoked()
    {
        var server = GetCodeRunner();

        var workspace = Workspace.FromSource(@"
using System;

public static class Hello
{
    public static void Main()
    {
        Console.WriteLine(""Hello there!"");
    }
}", workspaceType: "console");

        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        result.ShouldSucceedWithOutput("Hello there!");
    }

    [Fact]
    public async Task When_a_public_void_Main_with_parameters_is_present_it_is_invoked()
    {
        var server = GetCodeRunner();

        var workspace = Workspace.FromSource(@"
using System;

public static class Hello
{
    public static void Main(params string[] args)
    {
        Console.WriteLine(""Hello there!"");
    }
}", workspaceType: "console");

        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        result.ShouldSucceedWithOutput("Hello there!");
    }

    [Fact]
    public async Task When_an_internal_void_Main_with_no_parameters_is_present_it_is_invoked()
    {
        var server = GetCodeRunner();

        var workspace = Workspace.FromSource(@"
using System;

public static class Hello
{
    static void Main()
    {
        Console.WriteLine(""Hello there!"");
    }
}", workspaceType:"console");

        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        result.ShouldSucceedWithOutput("Hello there!");
    }

    [Fact]
    public async Task When_an_internal_void_Main_with_parameters_is_present_it_is_invoked()
    {
        var server = GetCodeRunner();

        var workspace = Workspace.FromSource(@"
using System;

public static class Hello
{
    static void Main(string[] args)
    {
        Console.WriteLine(""Hello there!"");
    }
}", workspaceType: "console");


        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        result.ShouldSucceedWithOutput("Hello there!");
    }


    [Fact]
    public async Task Response_shows_warnings_with_successful_compilation()
    {
        var output = nameof(Response_shows_warnings_with_successful_compilation);

        var server = GetCodeRunner();

        var workspace = CreateWorkspaceWithMainContaining($@"
using System;
using System;

public static class Hello
{{
    public static void Main()
    {{
        var a = 0;
        Console.WriteLine(""{output}"");
    }}
}}");

        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        var diagnostics = result.GetFeature<Diagnostics>();

        diagnostics.Should().Contain(d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public async Task Response_shows_warnings_when_compilation_fails()
    {
        var output = nameof(Response_shows_warnings_when_compilation_fails);

        var server = GetCodeRunner();

        var workspace = CreateWorkspaceWithMainContaining($@"
using System;

public static class Hello
{{
    public static void Main()
    {{
        var a = 0;
        Console.WriteLine(""{output}"")
    }}
}}");

        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        var diagnostics = result.GetFeature<Diagnostics>();

        diagnostics.Should().Contain(d => d.Severity == DiagnosticSeverity.Warning);
    }
}