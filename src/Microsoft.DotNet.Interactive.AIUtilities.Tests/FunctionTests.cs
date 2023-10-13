// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.AIUtilities.Tests;

public class FunctionTests
{
    private string DoCompute(int a, string b)
    {
        return $"{a} {b}";
    }

    [Fact]
    public void can_create_function_from_method_info()
    {
        var declaration = GPTFunctioDefinition.Do((int a, string b, string[]c) => $"{a} {b} {c}", "DoCompute");

        declaration.Should().Be("""
                                {
                                  "name": "DoCompute",
                                  "parameters": {
                                    "type": "object",
                                    "properties": {
                                      "a": {
                                        "type": "number"
                                      },
                                      "b": {
                                        "type": "string"
                                      },
                                      "c": {
                                        "type": "array",
                                        "items": {
                                          "type": "string"
                                        }
                                      }
                                    }
                                  },
                                  "results": {
                                    "type": "string"
                                  },
                                  "required": [
                                    "a",
                                    "b",
                                    "c"
                                  ]
                                }
                                """);

      
    }
}