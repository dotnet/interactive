// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharpProject.Build;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public class RoslynWorkspaceServerTestsConsoleProjectIntellisenseTests : RoslynWorkspaceServerTestsCore
{
    public RoslynWorkspaceServerTestsConsoleProjectIntellisenseTests(ITestOutputHelper output) : base(output)
    {
    }

    [Fact]
    public async Task Get_autocompletion_for_console_class()
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
                Cons$$
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
        var result = await server.GetCompletionsAsync(request);
        result.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().NotContain(signature => string.IsNullOrEmpty(signature.Kind));
        result.Items.Should().Contain(completion => completion.SortText == "Console");
        var hasDuplicatedEntries = HasDuplicatedCompletionItems(result);
        hasDuplicatedEntries.Should().BeFalse();
    }

    [Fact]
    public async Task Get_autocompletion_for_console_methods()
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
                Console.$$
            }
        }
    }
}".EnforceLF();

        #endregion

        var (processed, position) = CodeManipulation.ProcessMarkup(generator);

        var workspace = new Workspace(workspaceType: "console", buffers: new[]
        {
            new Buffer("Program.cs", program.EnforceLF()),
            new Buffer("generators/FibonacciGenerator.cs", processed, position)
        });

        var request = new WorkspaceRequest(workspace, activeBufferId: "generators/FibonacciGenerator.cs");
        var server = GetLanguageService();
        var result = await server.GetCompletionsAsync(request);

        result.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().NotContain(signature => string.IsNullOrEmpty(signature.Kind));
        result.Items.Should().Contain(completion => completion.SortText == "Beep");
        var hasDuplicatedEntries = HasDuplicatedCompletionItems(result);
        hasDuplicatedEntries.Should().BeFalse();
    }

    [Fact]
    public async Task Get_documentation_with_autocompletion_of_console_methods()
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
                Console.$$
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
        var result = await server.GetCompletionsAsync(request);

        result.Should().NotBeNull();
        result.Items.Should().NotBeNullOrEmpty();

        result.Items
            .Where(i => i.Documentation != null && !string.IsNullOrWhiteSpace(i.Documentation))
            .Select(i => i.Documentation)
            .Should()
            .Contain(d => d == "Writes the text representation of the specified Boolean value to the standard output stream.");
    }

    [Fact]
    public async Task Get_autocompletion_for_jtoken()
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                #region codeRegion
                #endregion
            }
        }
    }
}".EnforceLF();

        #endregion

        var (processed, position) = CodeManipulation.ProcessMarkup("JTo$$;");

        var workspace = new Workspace(
            workspaceType: "console",
            buffers: new[]
            {
                new Buffer("Program.cs", program.EnforceLF()),
                new Buffer("generators/FibonacciGenerator.cs@codeRegion", processed, position)
            }, files: new[]
            {
                new ProjectFileContent("generators/FibonacciGenerator.cs", generator.EnforceLF())
            });

        var request = new WorkspaceRequest(workspace, activeBufferId: "generators/FibonacciGenerator.cs@codeRegion");
        var server = GetLanguageService();
        var result = await server.GetCompletionsAsync(request);

        result.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().NotContain(signature => string.IsNullOrEmpty(signature.Kind));
        result.Items.Should().Contain(signature => signature.SortText == "JToken");
        var hasDuplicatedEntries = HasDuplicatedCompletionItems(result);
        hasDuplicatedEntries.Should().BeFalse();
    }

    [Fact]
    public async Task Get_autocompletion_for_jtoken_methods()
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                #region codeRegion
                #endregion
            }
        }
    }
}".EnforceLF();

        #endregion

        var (processed, position) = CodeManipulation.ProcessMarkup("JToken.fr$$;");

        var workspace = new Workspace(
            workspaceType: "console",
            buffers: new[]
            {
                new Buffer("Program.cs", program),
                new Buffer("generators/FibonacciGenerator.cs@codeRegion", processed, position)
            }, files: new[]
            {
                new ProjectFileContent("generators/FibonacciGenerator.cs", generator)
            });

        var request = new WorkspaceRequest(workspace, activeBufferId: "generators/FibonacciGenerator.cs@codeRegion");
        var server = GetLanguageService();
        var result = await server.GetCompletionsAsync(request);
        result.Items.Should().NotBeNullOrEmpty();
        result.Items.Should().NotContain(signature => string.IsNullOrEmpty(signature.Kind));
        result.Items.Should().Contain(signature => signature.SortText == "FromObject");
        var hasDuplicatedEntries = HasDuplicatedCompletionItems(result);
        hasDuplicatedEntries.Should().BeFalse();
    }

    [Fact]
    public async Task Get_autocompletion_can_be_empty()
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

        var generator = @"
#region codeRegion
#endregion
".EnforceLF();

        #endregion

        var (processed, position) = CodeManipulation.ProcessMarkup("class $$");

        var workspace = new Workspace(
            workspaceType: "console",
            buffers: new[] {
                new Buffer("Program.cs", program),
                new Buffer("generators/FibonacciGenerator.cs@codeRegion", processed, position)
            }, files: new[] {
                new ProjectFileContent("generators/FibonacciGenerator.cs", generator)
            });

        var request = new WorkspaceRequest(workspace, activeBufferId: "generators/FibonacciGenerator.cs@codeRegion");
        var server = GetLanguageService();
        var result = await server.GetCompletionsAsync(request);
        result.Items.Should().BeEmpty();
    }

    private static bool HasDuplicatedCompletionItems(CompletionResult result)
    {
        var g = result.Items.GroupBy(ci => ci.Kind + ci.InsertText).Select(cig => new { cig.Key, Count = cig.Count() });
        var duplicatedEntries = g.Where(cig => cig.Count > 1);
        return duplicatedEntries.Any();
    }

    [Fact]
    public async Task Get_signature_help_for_console_writeline()
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
        var result = await server.GetSignatureHelpAsync(request);

        result.Signatures.Should().NotBeNullOrEmpty();
        result.Signatures.Should().Contain(signature => signature.Label == "void Console.WriteLine(string format, params object?[]? arg)");
    }
        
    [Fact]
    public async Task Get_signature_help_for_invalid_location_return_empty()
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
                Console.WriteLine();$$
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
        var result = await server.GetSignatureHelpAsync(request);
        result.Should().NotBeNull();
        result.Signatures.Should().BeNullOrEmpty();
    }

    [Fact]
    public async Task Get_signature_help_for_console_writeline_with_region()
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
                #region codeRegion
                #endregion
            }
        }
    }
}".EnforceLF();

        #endregion

        var (processed, position) = CodeManipulation.ProcessMarkup("Console.WriteLine($$)");

        var workspace = new Workspace(
            workspaceType: "console",
            buffers: new[]
            {
                new Buffer("Program.cs", program),
                new Buffer("generators/FibonacciGenerator.cs@codeRegion", processed, position)
            }, files: new[]
            {
                new ProjectFileContent("generators/FibonacciGenerator.cs", generator),
            });

        var request = new WorkspaceRequest(workspace, activeBufferId: "generators/FibonacciGenerator.cs@codeRegion");
        var server = GetLanguageService();
        var result = await server.GetSignatureHelpAsync(request);

        result.Signatures.Should().NotBeNullOrEmpty();
        result.Signatures.Should().Contain(signature => signature.Label == "void Console.WriteLine(string format, params object?[]? arg)");
    }

    [Fact]
    public async Task Get_signature_help_for_jtoken()
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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                #region codeRegion
                #endregion
            }
        }
    }
}".EnforceLF();

        #endregion

        var (processed, position) = CodeManipulation.ProcessMarkup("JToken.FromObject($$);");

        var workspace = new Workspace(
            workspaceType: "console",
            buffers: new[]
            {
                new Buffer("Program.cs", program),
                new Buffer("generators/FibonacciGenerator.cs@codeRegion", processed, position)
            }, files: new[]
            {
                new ProjectFileContent("generators/FibonacciGenerator.cs", generator),
            });

        var request = new WorkspaceRequest(workspace, activeBufferId: "generators/FibonacciGenerator.cs@codeRegion");
        var server = GetLanguageService();
        var result = await server.GetSignatureHelpAsync(request);

        result.Signatures.Should().NotBeNullOrEmpty();
        result.Signatures.Should().Contain(signature => signature.Label == "JToken JToken.FromObject(object o)");
    }

    [Fact]
    public async Task Get_documentation_with_signature_help_for_console_writeline()
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
                Console.WriteLine($$);
            }
        }
    }
}".EnforceLF();

        #endregion

        var (processed, position) = CodeManipulation.ProcessMarkup(generator);
        var package = await PackageUtilities.CreateBuildableCopy(await Package.GetOrCreateConsolePackageAsync(false));
        var workspace = new Workspace(workspaceType: package.Name, buffers: new[]
        {
            new Buffer("Program.cs", program),
            new Buffer("generators/FibonacciGenerator.cs", processed, position)
        });

        var request = new WorkspaceRequest(workspace, activeBufferId: "generators/FibonacciGenerator.cs");
        var server = GetLanguageService();
        var result = await server.GetSignatureHelpAsync(request);

        result.Signatures.Should().NotBeNullOrEmpty();

        var sample = result.Signatures.First(e => e.Label == "void Console.WriteLine(string format, params object?[]? arg)");
        sample.Documentation.Value.Should().Contain("Writes the text representation of the specified array of objects, followed by the current line terminator, to the standard output stream using the specified format information.");
        sample.Parameters.Should().HaveCount(2);

        sample.Parameters.ElementAt(0).Label.Should().Be("string format");
        sample.Parameters.ElementAt(0).Documentation.Value.Should().Contain("A composite format string.");

        sample.Parameters.ElementAt(1).Label.Should().Be("params object?[]? arg");
        sample.Parameters.ElementAt(1).Documentation.Value.Should().Contain("An array of objects to write using format .");
    }
}