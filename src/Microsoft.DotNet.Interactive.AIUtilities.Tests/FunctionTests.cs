﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
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
        var declaration = GptFunction.Create("DoCompute", (int a, string b, string[] c) => {});

        declaration.JsonSignature.FormatJson().Should().Be("""
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
        var declaration = GptFunction.Create("DoCompute", (int a, string b, string[] c) => new Uri($"{a}.{b}.{c}"));

        declaration.JsonSignature.FormatJson().Should().Be("""
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
    public void can_create_function_from_delegate_with_enums_as_parameters()
    {
        var declaration = GptFunction.Create("DoCompute", (int a, double b, EnumType c) => $"{a} {b} {c}");

        declaration.JsonSignature.FormatJson().Should().Be("""
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
    public void can_create_function_from_delegate_with_enums_strings_as_parameters()
    {
        var declaration = GptFunction.Create("DoCompute", (int a, double b, EnumType[] c) => $"{a} {b} {c}", enumsAsString:true);

        declaration.JsonSignature.FormatJson().Should().Be("""
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
                                                                   "type": "array",
                                                                   "items": {
                                                                     "type": "string",
                                                                     "enum": [
                                                                       "One",
                                                                       "Two",
                                                                       "Three",
                                                                       "Four"
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
    public void can_create_function_from_delegate_with_array_of_enums_as_parameters()
    {
        var declaration = GptFunction.Create("DoCompute", (byte a, bool b, EnumType[] c) => $"{a} {b} {c}");

        declaration.JsonSignature.FormatJson().Should().Be("""
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
        var function = GptFunction.Create("concatString", (string a, double b) => $"{a} {b}");

        var jsonArgs = """
                        {
                            "name": "concatString",
                            "arguments": "{ \"a\": \"Diego\", \"b\":123.0}"
                        }
                       """;
        
        var result = function.Execute(jsonArgs);
        result.Should().Be("Diego 123");
    }

    [Fact]
    public void can_invoke_function_with__enum()
    {
        var function = GptFunction.Create("concatString", (string a, EnumType b) => $"{a} {b}");

        var jsonArgs = """
                        {
                            "name": "concatString",
                            "arguments": "{ \"a\": \"Diego\", \"b\":0}"
                        }
                       """;

        var result = function.Execute(jsonArgs);
        result.Should().Be("Diego One");
    }

    [Fact]
    public void can_invoke_function_with_string_for_enum()
    {
        var function = GptFunction.Create("concatString", (string a, EnumType b) => $"{a} {b}");

        var jsonArgs = """
                        {
                            "name": "concatString",
                            "arguments": "{ \"a\": \"Diego\", \"b\":\"Three\"}"
                        }
                       """;

        var result = function.Execute(jsonArgs);
        result.Should().Be("Diego Three");
    }

    [Fact]
    public void can_invoke_function_with_string_for_enum_array()
    {
        var function = GptFunction.Create("concatString", (string a, EnumType[] b) => $"{a} {string.Join(",", b.Select(b => b.ToString()))}");

        var jsonArgs = """
                        {
                            "name": "concatString",
                            "arguments": "{ \"a\": \"Diego\", \"b\":[\"Three\",\"Three\"]}"
                        }
                       """;

        var result = function.Execute(jsonArgs);
        result.Should().Be("Diego Three,Three");
    }

    [Fact]
    public void can_invoke_function_from_json_string()
    {
        var function = GptFunction.Create("concatString", (string a, EnumType[] b) => $"{a} {string.Join(",", b.Select(b => b.ToString()))}");

        var jsonArgs = """
                       { "a" : "Diego", "b":["Three","Three"]}
                       """;

        var result = function.Execute(jsonArgs);
        result.Should().Be("Diego Three,Three");
    }

    [Fact]
    public async Task can_invoke_async_function()
    {
        var function = GptFunction.Create("concatString", async(string a, EnumType[] b) =>
        {
            await Task.Yield();
            return $"{a} {string.Join(",", b.Select(b => b.ToString()))}";
        });

        var jsonArgs = """
                        {
                            "name": "concatString",
                            "arguments": "{ \"a\": \"Diego\", \"b\":[\"Three\",\"Three\"]}"
                        }
                       """;

        var result = await (Task<string>)function.Execute(jsonArgs);
        result.Should().Be("Diego Three,Three");
    }


    //[Fact(Skip = "requires new language version")]
    //public void can_invoke_function_with_optional_parameters()
    //{
    //    var function = GptFunction.Create("inc", (int a, int b = 1) => a+b);

    //    var jsonArgs = """
    //                    {
    //                        "name": "inc",
    //                        "arguments": "{ \"a\": 23}"
    //                    }
    //                   """;

    //    var result = function.Execute(jsonArgs);
    //    result.Should().Be(24);
    //}
}

internal static class JsonFormatting
{
    public static string FormatJson(this string text)
    {
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        var element = JsonDocument.Parse(text).RootElement;
        return JsonSerializer.Serialize(element,
            jsonSerializerOptions);
    }
}