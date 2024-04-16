// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class RoslynWorkspaceServerTestsConsoleProjectDiagnosticsTests : WorkspaceServerTestsCore
{
    public RoslynWorkspaceServerTestsConsoleProjectDiagnosticsTests(PrebuildFixture prebuildFixture, ITestOutputHelper output) : base(prebuildFixture, output)
    {
    }

    [Fact]
    public async Task Get_diagnostics_with_buffer_with_region()
    {
        #region bufferSources

        var program = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            #region code
            #endregion
        }
    }
}".EnforceLF();

        var region = @"adddd".EnforceLF();

        #endregion

        var (processed, position) = CodeManipulation.ProcessMarkup(region);

        var workspace = new Workspace(workspaceType: "console", buffers: new[]
        {
            new Buffer("Program.cs", program),
            new Buffer(new BufferId( "Program.cs","code"), processed, position)
        });

        var request = new WorkspaceRequest(workspace, activeBufferId: new BufferId("Program.cs", "code"));
        var server = GetLanguageService();
        var result = await server.GetDiagnosticsAsync(request);

        result.Diagnostics.Should().NotBeNullOrEmpty();
        result.Diagnostics.Should().Contain(diagnostics => diagnostics.Message == "(1,1): error CS0103: The name 'adddd' does not exist in the current context");
    }

    [Fact]
    public async Task Get_diagnostics_with_buffer_with_region_in_code_but_not_in_buffer_id()
    {
        #region bufferSources

        var program = @"using System;
using System.Linq;

namespace FibonacciTest
{
    public class Program
    {
        public static void Main()
        {
            #region code
            error
            #endregion
            moreError
        }
    }
}".EnforceLF();

        #endregion

        var (processed, position) = CodeManipulation.ProcessMarkup(program);

        var workspace = new Workspace(workspaceType: "console", buffers: new[]
        {
            new Buffer("Program.cs", program),
        });

        var request = new WorkspaceRequest(workspace, activeBufferId: "Program.cs");
        var server = GetLanguageService();
        var result = await server.GetDiagnosticsAsync(request);

        result.Diagnostics.Should().NotBeNullOrEmpty();
        result.Diagnostics.Should().Contain(diagnostics => diagnostics.Message == "Program.cs(13,13): warning CS0168: The variable 'moreError' is declared but never used");
    }

    [Fact]
    public async Task Get_diagnostics()
    {
        #region bufferSources

        var program = @"using System;
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
}".EnforceLF();

        var generator = @"using System.Collections.Generic;
using System;
namespace FibonacciTest
{
    public static class FibonacciGenerator
    {
        public static IEnumerable<int> Fibonacci()
        {
            int current = 1, next = 1;
            while (true)
            {
                yield return current;
                next = current + (current = next);
                adddd
                Console.WriteLine($$);
            }
        }
    }
}".EnforceLF();

        #endregion

        var (processed, position) = CodeManipulation.ProcessMarkup(generator);

        var workspace = new Workspace(workspaceType: "console", buffers: new[]
        {
            new Buffer("Program.cs", program),
            new Buffer("generators/FibonacciGenerator.cs", processed, position)
        });

        var request = new WorkspaceRequest(workspace, activeBufferId: "generators/FibonacciGenerator.cs");
        var server = GetLanguageService();
        var result = await server.GetDiagnosticsAsync(request);

        result.Diagnostics.Should().NotBeNullOrEmpty();
        result.Diagnostics.Should().Contain(diagnostics => diagnostics.Message == "generators/FibonacciGenerator.cs(14,17): error CS0246: The type or namespace name \'adddd\' could not be found (are you missing a using directive or an assembly reference?)");
    }
}