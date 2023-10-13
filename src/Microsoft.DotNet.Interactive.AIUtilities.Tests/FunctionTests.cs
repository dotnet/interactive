// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    public void can_create_function_from_delegate_with_no_return()
    {
        var declaration = GPTFunctionDefinition.Create((int a, string b, string[] c) => {}, "DoCompute");

        declaration.JsonSignature.Should().Be("""
                                {
                                  "name": "DoCompute",
                                  "parameters": {
                                    "type": "object",
                                    "properties": {
                                      "a": {
                                        "type": "integer"
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
                                  "required": [
                                    "a",
                                    "b",
                                    "c"
                                  ]
                                }
                                """);


    }

    [Fact]
    public void can_create_function_from_delegate_with_complex_type_as_return()
    {
        var declaration = GPTFunctionDefinition.Create((int a, string b, string[] c) => new Uri($"{a}.{b}.{c}"), "DoCompute");

        declaration.JsonSignature.Should().Be("""
                                              {
                                                "name": "DoCompute",
                                                "parameters": {
                                                  "type": "object",
                                                  "properties": {
                                                    "a": {
                                                      "type": "integer"
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
                                                  "type": "object"
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
    public void can_create_function_from_delegate()
    {
        var declaration = GPTFunctionDefinition.Create((int a, string b, string[]c) => $"{a} {b} {c}", "DoCompute");

        declaration.JsonSignature.Should().Be("""
                                {
                                  "name": "DoCompute",
                                  "parameters": {
                                    "type": "object",
                                    "properties": {
                                      "a": {
                                        "type": "integer"
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
        var declaration = GPTFunctionDefinition.Create((int a, double b, EnumType c) => $"{a} {b} {c}", "DoCompute");

        declaration.JsonSignature.Should().Be("""
                                {
                                  "name": "DoCompute",
                                  "parameters": {
                                    "type": "object",
                                    "properties": {
                                      "a": {
                                        "type": "integer"
                                      },
                                      "b": {
                                        "type": "number"
                                      },
                                      "c": {
                                        "type": "integer",
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
        var declaration = GPTFunctionDefinition.Create((byte a, bool b, EnumType[] c) => $"{a} {b} {c}", "DoCompute");

        declaration.JsonSignature.Should().Be("""
                                {
                                  "name": "DoCompute",
                                  "parameters": {
                                    "type": "object",
                                    "properties": {
                                      "a": {
                                        "type": "integer"
                                      },
                                      "b": {
                                        "type": "boolean"
                                      },
                                      "c": {
                                        "type": "array",
                                        "items": {
                                          "type": "integer",
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

    [Fact]
    public void can_invoke_function()
    {
        var function = GPTFunctionDefinition.Create((string a, double b) => $"{a} {b}", "concatString");

        var jsonArgs = """
                        {
                            "name": "concatString",
                            "arguments": "{ \"a\": \"Diego\", \"b\":123.0}"
                        }
                       """;
        
        function.Execute<string>(jsonArgs, out var result);
        result.Should().Be("Diego 123");
    }


    [Fact]
    public void can_invoke_function_with_optional_paramaters()
    {
        var function = GPTFunctionDefinition.Create((int a, int b = 1) => a+b, "inc");

        var jsonArgs = """
                        {
                            "name": "inc",
                            "arguments": "{ \"a\": 23}"
                        }
                       """;

        function.Execute<int>(jsonArgs, out var result);
        result.Should().Be(24);
    }
}