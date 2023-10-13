// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.AIUtilities.Tests;

public class FunctionTests
{
    public enum EnumType
    {
        One,
        Two, 
        Three, 
        Four
    }

    [Fact]
    public void can_create_function_from_delegate()
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

    [Fact]
    public void can_create_function_from_delegate_with_enums_as_parameters()
    {
        var declaration = GPTFunctioDefinition.Do((int a, string b, EnumType c) => $"{a} {b} {c}", "DoCompute");

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
                                        "type": "number",
                                        "enum": [
                                          0,
                                          1,
                                          2,
                                          3
                                        ]
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

    [Fact]
    public void can_create_function_from_delegate_with_array_of_enums_as_parameters()
    {
        var declaration = GPTFunctioDefinition.Do((int a, string b, EnumType[] c) => $"{a} {b} {c}", "DoCompute");

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
                                          "type": "number",
                                          "enum": [
                                            0,
                                            1,
                                            2,
                                            3
                                          ]
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