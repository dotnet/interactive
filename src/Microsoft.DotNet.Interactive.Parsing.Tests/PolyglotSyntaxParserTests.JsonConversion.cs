// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    public class JsonConversion
    {
        [Fact]
        public async Task Directive_nodes_not_containing_JSON_can_be_converted_to_JSON()
        {
            var tree = Parse("""
                #!connect mssql --connection-string "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost; Encrypt=false"
                """);

            var directiveNode = tree.RootNode.ChildNodes
                                    .Should().ContainSingle<DirectiveNode>()
                                    .Which;

            var result = await directiveNode.TryGetJsonAsync(node => null);

            var json = result.Should().BeOfType<DirectiveBindingResult<string>>().Which.Value;

            json.Should().BeEquivalentJsonTo("""
                {
                  "commandType": "ConnectMsSql",
                  "command": {
                    "directive": { 
                        "command": "#!connect mssql",
                        "connectionString": "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost; Encrypt=false"
                    },
                    "targetKernelName": ".NET"
                  }
                }
                """);

            // TODO (Directives_are_deserialized_as_JSON) write test
            throw new NotImplementedException();
        }
    }
}