// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;
using Pocket;
using Recipes;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Scripting;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;
using Xunit;
using Xunit.Abstractions;
using Buffer = Microsoft.DotNet.Interactive.CSharpProject.Protocol.Buffer;
using Package = Microsoft.DotNet.Interactive.CSharpProject.Packaging.Package;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public class ScriptingWorkspaceServerTests : WorkspaceServerTests
    {
        public ScriptingWorkspaceServerTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override Workspace CreateWorkspaceWithMainContaining(string text) =>
            Workspace.FromSource(text, workspaceType: "script");
      

        protected override ILanguageService GetLanguageService() =>
            throw new NotImplementedException();

        protected override ICodeCompiler GetCodeCompiler() => throw new NotImplementedException();

        protected override ICodeRunner GetCodeRunner() => new ScriptingWorkspaceServer();

        [Fact]
        public async Task Response_shows_fragment_return_value()
        {
            var workspace =
                Workspace.FromSource(@"
var person = new { Name = ""Jeff"", Age = 20 };
$""{person.Name} is {person.Age} year(s) old""", "script");

            var server = GetCodeRunner();

            var result = await server.Run(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = true,
                Output = new string[] { },
                Exception = (string)null,
                ReturnValue = $"Jeff is 20 year(s) old",
            }, config => config.ExcludingMissingMembers());
        }

        [Fact]
        public async Task Response_indicates_when_compile_is_unsuccessful()
        {
            var workspace = Workspace.FromSource(@"
Console.WriteLine(banana);", "script");
            var server = GetCodeRunner();

            var result = await server.Run(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = false,
                Output = new[] { "(2,19): error CS0103: The name \'banana\' does not exist in the current context" },
                Exception = (string)null, // we already display the error in Output
            }, config => config.ExcludingMissingMembers());
        }

        [Fact]
        public async Task Additional_using_statements_from_request_are_passed_to_scripting_when_running_snippet()
        {
            var workspace = Workspace.FromSource(
                @"
using System;

public static class Hello
{
    public static void Main()
    {
        Thread.Sleep(1);
        Console.WriteLine(""Hello there!"");
    }
}",
                workspaceType: "script",
                usings: new[] { "System.Threading" });

            var server = GetCodeRunner();

            var result = await server.Run(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = true,
                Output = new[] { "Hello there!", "" },
                Exception = (string)null,
            }, config => config.ExcludingMissingMembers());
        }

        [Fact]
        public async Task When_a_public_void_Main_with_non_string_parameters_is_present_it_is_not_invoked()
        {
            var workspace = Workspace.FromSource(@"
using System;

public static class Hello
{
    public static void Main(params int[] args)
    {
        Console.WriteLine(""Hello there!"");
    }
}", workspaceType: "script");
            var server = GetCodeRunner();

            var result = await server.Run(new WorkspaceRequest(workspace));

            result.ShouldSucceedWithNoOutput();
        }

        [Fact]
        public async Task CS7022_not_reported_for_main_in_global_script_code()
        {
            var workspace = Workspace.FromSource(@"
using System;

public static class Hello
{
    public static void Main()
    {
        Console.WriteLine(""Hello there!"");
    }
}", workspaceType: "script");
            var server = GetCodeRunner();

            var result = await server.Run(new WorkspaceRequest(workspace));

            var diagnostics = result.GetFeature<Diagnostics>();

            diagnostics.Should().NotContain(d => d.Id == "CS7022");
        }

        [Fact]
        public async Task When_using_buffers_they_are_inlined()
        {
            var fileCode = @"using System;

public static class Hello
{
    static void Main(string[] args)
    {
        #region toReplace
        Console.WriteLine(""Hello world!"");
        #endregion
    }
}";
            var workspace = new Workspace(
                workspaceType: "script",
                files: new[] { new File("Main.cs", fileCode) },
                buffers: new[] { new Buffer(@"Main.cs@toReplace", @"Console.WriteLine(""Hello there!"");", 0) });
            var server = GetCodeRunner();

            var result = await server.Run(new WorkspaceRequest(workspace));

            result.ShouldSucceedWithOutput("Hello there!");
        }

        [Fact]
        public async Task When_using_buffers_they_are_inlined_and_warnings_are_aligned()
        {
            var fileCode = @"using System;

public static class Hello
{
    static void Main(string[] args)
    {
        #region toReplace
        Console.WriteLine(""Hello world!"");
        #endregion
    }
}";
            var workspace = new Workspace(
                workspaceType: "script",
                files: new[] { new File("Main.cs", fileCode) },
                buffers: new[] { new Buffer(@"Main.cs@toReplace", @"Console.WriteLine(banana);", 0) });

            var server = GetCodeRunner();
            var result = await server.Run(new WorkspaceRequest(workspace));

            result.Should().BeEquivalentTo(new
            {
                Succeeded = false,
                Output = new[] { $"(1,19): error CS0103: The name \'banana\' does not exist in the current context" },
                Exception = (string)null,
            }, config => config.ExcludingMissingMembers());
        }
    }
}
