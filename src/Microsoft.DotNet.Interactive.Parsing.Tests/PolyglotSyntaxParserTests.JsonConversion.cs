// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    public class JsonConversion
    {
        private readonly ITestOutputHelper _output;

        public JsonConversion(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Directive_nodes_not_containing_JSON_can_be_converted_to_JSON()
        {
            var tree = Parse("""
                #!connect mssql  --connection-string "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost; Encrypt=false" --kernel-name sql-adventureworks 
                """);

            var directiveNode = tree.RootNode.ChildNodes
                                    .Should().ContainSingle<DirectiveNode>()
                                    .Which;

            var result = await directiveNode.TryGetJsonAsync();

            result.Should().BeOfType<DirectiveBindingResult<string>>();

            _output.WriteLine(tree.RootNode.Diagram());

            result.Diagnostics.Should().BeEmpty();

            result.Value.Should().BeEquivalentJsonTo("""
                {
                  "commandType": "ConnectMsSql",
                  "command": {
                    "invokedDirective": "#!connect mssql",
                    "kernelName": "sql-adventureworks",
                    "connectionString": "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost; Encrypt=false",
                    "targetKernelName": ".NET"
                  }
                }
                """);
        }
    }
}