// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Pocket;
using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class WorkspaceServerExecutionTests : WorkspaceServerTestsCore
{
    public WorkspaceServerExecutionTests(PrebuildFixture prebuildFixture, ITestOutputHelper output) : base(prebuildFixture, output)
    {
    }

    protected Workspace CreateWorkspaceWithMainContaining(string text)
    {
        return Workspace.FromSource(
            $@"using System; using System.Linq; using System.Collections.Generic; class Program {{ static void Main() {{ {text}
                    }}
                }}
            ",
            workspaceType: "console");
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
                        "Console.WriteLine(\"hi!\");")));

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
}", workspaceType: "console");

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


    [Fact]
    public async Task Run_returns_emoji()
    {
        var server = GetCodeRunner();

        var workspace = new Workspace(
            workspaceType: "console",
            files: new[] { new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
            buffers: new[] { new Buffer("Program.cs@alpha", @"Console.OutputEncoding = System.Text.Encoding.UTF8;Console.WriteLine(""😊"");", 0) });

        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        result.Should().BeEquivalentTo(new
        {
            Succeeded = true,
            Output = new[] { "😊", "" },
            Exception = (string)null, // we already display the error in Output
        }, config => config.ExcludingMissingMembers());
    }

    [Fact]
    public async Task When_run_fails_to_compile_then_diagnostics_are_aligned_with_buffer_span()
    {
        var server = GetCodeRunner();

        var workspace = new Workspace(
            workspaceType: "console",
            files: new[] { new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
            buffers: new[] { new Buffer("Program.cs@alpha", @"Console.WriteLine(banana);", 0) });


        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        result.Should().BeEquivalentTo(new
        {
            Succeeded = false,
            Output = new[] { "(1,19): error CS0103: The name \'banana\' does not exist in the current context" },
            Exception = (string)null, // we already display the error in Output
        }, config => config.ExcludingMissingMembers());
    }

    [Fact]
    public async Task When_run_fails_to_compile_then_diagnostics_are_aligned_with_buffer_span_when_code_is_multi_line()
    {
        var server = GetCodeRunner();

        var workspace = new Workspace(
            workspaceType: "console",
            files: new[] { new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
            buffers: new[] { new Buffer("Program.cs@alpha", @"var a = 10;" + Environment.NewLine + "Console.WriteLine(banana);", 0) });

        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        result.Should().BeEquivalentTo(new
        {
            Succeeded = false,
            Output = new[] { "(2,19): error CS0103: The name \'banana\' does not exist in the current context" },
            Exception = (string)null, // we already display the error in Output
        }, config => config.ExcludingMissingMembers());
    }

    [Fact]
    public async Task When_diagnostics_are_outside_of_viewport_then_they_are_omitted()
    {
        var server = GetCodeRunner();

        var workspace = new Workspace(
            workspaceType: "console",
            files: new[] { new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegionExtraUsing) },
            buffers: new[] { new Buffer("Program.cs@alpha", @"var a = 10;" + Environment.NewLine + "Console.WriteLine(a);", 0) });

        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        result.GetFeature<Diagnostics>().Should().BeEmpty();
    }

    [Fact]
    public async Task When_compile_fails_then_diagnostics_are_aligned_with_buffer_span()
    {
        var server = GetCodeCompiler();

        var workspace = new Workspace(
            workspaceType: "console",
            files: new[] { new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
            buffers: new[] { new Buffer("Program.cs@alpha", @"Console.WriteLine(banana);", 0) });


        var result = await server.CompileAsync(new WorkspaceRequest(workspace));

        result.Should().BeEquivalentTo(new
        {
            Succeeded = false,
            Output = new[] { "(1,19): error CS0103: The name \'banana\' does not exist in the current context" },
            Exception = (string)null, // we already display the error in Output
        }, config => config.ExcludingMissingMembers());
    }

    [Fact]
    public async Task When_compile_fails_then_diagnostics_are_aligned_with_buffer_span_when_code_is_multi_line()
    {
        var server = GetCodeCompiler();

        var workspace = new Workspace(
            workspaceType: "console",
            files: new[] { new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion) },
            buffers: new[] { new Buffer("Program.cs@alpha", @"var a = 10;" + Environment.NewLine + "Console.WriteLine(banana);", 0) });

        var result = await server.CompileAsync(new WorkspaceRequest(workspace));

        result.Should().BeEquivalentTo(new
        {
            Succeeded = false,
            Output = new[] { "(2,19): error CS0103: The name \'banana\' does not exist in the current context" },
            Exception = (string)null, // we already display the error in Output
        }, config => config.ExcludingMissingMembers());
    }

    [Fact]
    public async Task When_compile_diagnostics_are_outside_of_viewport_then_they_are_omitted()
    {
        var server = GetCodeCompiler();

        var workspace = new Workspace(
            workspaceType: "console",
            files: new[] { new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegionExtraUsing) },
            buffers: new[] { new Buffer("Program.cs@alpha", @"var a = 10;" + Environment.NewLine + "Console.WriteLine(a);", 0) });

        var result = await server.CompileAsync(new WorkspaceRequest(workspace));

        result.GetFeature<Diagnostics>().Should().BeEmpty();
    }

    [Fact]
    public async Task When_compile_diagnostics_are_outside_of_active_file_then_they_are_omitted()
    {
        #region bufferSources

        const string program = @"
using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
        }
    }
}";
        const string generator = @"
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
        #endregion

        var server = GetCodeCompiler();

        var request = new WorkspaceRequest(
            new Workspace(
                workspaceType: "console",
                buffers: new[]
                {
                    new Buffer("Program.cs", program, 0),
                    new Buffer("FibonacciGenerator.cs", generator, 0)
                }),
            new BufferId("Program.cs"));

        var result = await server.CompileAsync(request);

        result.GetFeature<Diagnostics>().Should().BeEmpty();
    }

    [Fact]
    public async Task When_diagnostics_are_outside_of_active_file_then_they_are_omitted()
    {
        #region bufferSources

        const string program = @"
using System.Collections.Generic;
using System.Linq;
using System;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
        }
    }
}";
        const string generator = @"
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
        #endregion

        var server = GetCodeRunner();

        var request = new WorkspaceRequest(
            new Workspace(
                workspaceType: "console",
                buffers: new[]
                {
                    new Buffer("Program.cs", program, 0),
                    new Buffer("FibonacciGenerator.cs", generator, 0)
                }),
            new BufferId("Program.cs"));

        var result = await server.RunAsync(request);

        result.GetFeature<Diagnostics>().Should().BeEmpty();
    }

    [Fact]
    public async Task When_compile_is_unsuccessful_and_there_are_multiple_buffers_with_errors_then_diagnostics_for_both_buffers_are_displayed_in_output()
    {
        #region bufferSources

        const string programWithCompileError = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i)          DOES NOT COMPILE
            }
        }
    }
}";
        const string generatorWithCompileError = @"using System.Collections.Generic;
namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;        DOES NOT COMPILE
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
        #endregion

        var server = GetCodeRunner();

        var request = new WorkspaceRequest(
            new Workspace(
                workspaceType: "console",
                buffers: new[]
                {
                    new Buffer("Program.cs", programWithCompileError),
                    new Buffer("FibonacciGenerator.cs", generatorWithCompileError)
                }),
            new BufferId("FibonacciGenerator.cs"));

        var result = await server.RunAsync(request);
        result.Succeeded.Should().BeFalse();

        result.Output
            .Should()
            .BeEquivalentTo(
                "FibonacciGenerator.cs(8,47): error CS0246: The type or namespace name 'DOES' could not be found (are you missing a using directive or an assembly reference?)",
                "FibonacciGenerator.cs(8,56): error CS0103: The name 'COMPILE' does not exist in the current context",
                "FibonacciGenerator.cs(8,56): error CS1002: ; expected",
                "FibonacciGenerator.cs(8,63): error CS1002: ; expected",
                "Program.cs(12,47): error CS1002: ; expected",
                "Program.cs(12,47): error CS0246: The type or namespace name 'DOES' could not be found (are you missing a using directive or an assembly reference?)",
                "Program.cs(12,56): error CS0103: The name 'COMPILE' does not exist in the current context",
                "Program.cs(12,56): error CS1002: ; expected",
                "Program.cs(12,63): error CS1002: ; expected");
    }

    [Fact]
    public async Task When_compile_is_unsuccessful_and_there_are_multiple_masked_buffers_with_errors_then_diagnostics_for_both_buffers_are_displayed_in_output()
    {
        #region bufferSources

        const string programWithCompileError = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
#region mask
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
#endregion
        }
    }
}";
        const string generatorWithCompileError = @"using System.Collections.Generic;
namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public static IEnumerable<int> Fibonacci()           
        {
#region mask
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
#endregion
        }
    }
}";
        #endregion

        var server = GetCodeRunner();

        var request = new WorkspaceRequest(
            new Workspace(
                workspaceType: "console",

                files: new[]
                {
                    new ProjectFileContent("Program.cs", programWithCompileError),
                    new ProjectFileContent("FibonacciGenerator.cs", generatorWithCompileError),
                },
                buffers: new[]
                {
                    new Buffer("Program.cs@mask", "WAT"),
                    new Buffer("FibonacciGenerator.cs@mask", "HUH"),
                }),
            new BufferId("FibonacciGenerator.cs", "mask2"));

        var result = await server.RunAsync(request);
        result.Succeeded.Should().BeFalse();

        Logger.Log.Info("OUTPUT:\n{output}", result.Output);

        result.Output
            .Should()
            .Contain(line => line.Contains("WAT"))
            .And
            .Contain(line => line.Contains("HUH"));
    }

    [Fact]
    public async Task Response_with_multi_buffer_workspace()
    {
        #region bufferSources

        const string program = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
        }
    }
}";
        const string generator = @"using System.Collections.Generic;

namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
        #endregion

        var server = GetCodeRunner();

        var workspace = new Workspace(workspaceType: "console", buffers: new[]
        {
            new Buffer("Program.cs", program, 0),
            new Buffer("FibonacciGenerator.cs", generator, 0)
        });

        var result = await server.RunAsync(new WorkspaceRequest(workspace, BufferId.Parse("Program.cs")));

        result.Succeeded.Should().BeTrue();
        result.Output.Count.Should().Be(21);
        result.Output.Should().BeEquivalentTo("1", "1", "2", "3", "5", "8", "13", "21", "34", "55", "89", "144", "233", "377", "610", "987", "1597", "2584", "4181", "6765", "");
    }

    [Fact]
    public async Task Response_with_multi_buffer_using_relative_paths_workspace()
    {
        #region bufferSources

        const string program = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
        }
    }
}";
        const string generator = @"using System.Collections.Generic;

namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
        #endregion

        var server = GetCodeRunner();

        var workspace = new Workspace(workspaceType: "console", buffers: new[]
        {
            new Buffer("Program.cs", program, 0),
            new Buffer("generators/FibonacciGenerator.cs", generator, 0)
        });

        var result = await server.RunAsync(new WorkspaceRequest(workspace, BufferId.Parse("Program.cs")));

        result.ShouldSucceedWithOutput("1", "1", "2", "3", "5", "8", "13", "21", "34", "55", "89", "144", "233", "377", "610", "987", "1597", "2584", "4181", "6765", "");
    }

    [Fact]
    public async Task Compile_response_with_multi_buffer_using_relative_paths_workspace()
    {
        #region bufferSources

        const string program = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            foreach (var i in FibonacciGenerator.Fibonacci().Take(20))
            {
                Console.WriteLine(i);
            }
        }
    }
}";
        const string generator = @"using System.Collections.Generic;

namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public  static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
            }
        }
    }
}";
        #endregion

        var server = GetCodeCompiler();

        var workspace = new Workspace(workspaceType: "console", buffers: new[]
        {
            new Buffer("Program.cs", program, 0),
            new Buffer("generators/FibonacciGenerator.cs", generator, 0)
        });

        var result = await server.CompileAsync(new WorkspaceRequest(workspace, BufferId.Parse("Program.cs")));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Can_compile_c_sharp_8_features()
    {
        var server = GetCodeRunner();

        var workspace = Workspace.FromSource(@"
using System;

public static class Hello
{
    public static void Main()
    {
        var i1 = 3;  // number 3 from beginning
        var i2 = ^4; // number 4 from end
        var a = new[]{ 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        Console.WriteLine($""{a[i1]}, {a[i2]}"");
    }
}
", workspaceType: "console");

        var result = await server.RunAsync(new WorkspaceRequest(workspace));

        result.Output.ShouldMatch("3, 6");
    }
}
