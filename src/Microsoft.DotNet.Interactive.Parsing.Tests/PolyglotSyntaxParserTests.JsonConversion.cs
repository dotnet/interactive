// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    public class JsonConversion
    {

        [Fact]
        public async Task Directive_nodes_not_containing_JSON_can_be_converted_to_JSON()
        {
           
            // TODO (Directives_are_deserialized_as_JSON) write test
            throw new NotImplementedException();
        }

    }
}