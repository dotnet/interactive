// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Newtonsoft.Json.Linq;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests.Instrumentation
{
    public class ArgumentListGeneratorTests
    {
        [Fact]
        public void CreateSyntaxNode_works_correctly()
        {
            var a = new { foo = 3 };
            var vi = new VariableInfo
            {
                Name = nameof(a),
                Value = JToken.FromObject(a),
                RangeOfLines = new LineRange
                {
                    Start = 10,
                    End = 11
                }
            };
            var filePosition = new FilePosition
            {
                Line = 1,
                Character = 1,
                File = "test.cs"
            };

            var result = InstrumentationSyntaxRewriter.CreateSyntaxNode(filePosition, vi).ToString();

            var emitterTypeName = typeof(InstrumentationEmitter).FullName;

            var expected =
                $"{emitterTypeName}.EmitProgramState({emitterTypeName}.GetProgramState(\"{{\\\"line\\\":1,\\\"character\\\":1,\\\"file\\\":\\\"test.cs\\\"}}\",(\"{{\\\"name\\\":\\\"a\\\",\\\"value\\\":{{\\\"foo\\\":3}},\\\"declaredAt\\\":{{\\\"start\\\":10,\\\"end\\\":11}}}}\",a)));";
            result.Should().Be(expected);
        }

        [Fact]
        public void It_can_pass_through_an_argument()
        {
            var argument = new { foo = 3 };

            var list = ArgumentListGenerator.GenerateArgumentListForGetProgramState(new FilePosition
            {
                Line = 1,
                Character = 1,
                File = "test.cs"
            },
            (argument, "foo"));

            var text = list.ToString();
            var expected = "(\"{\\\"line\\\":1,\\\"character\\\":1,\\\"file\\\":\\\"test.cs\\\"}\",(\"{\\\"foo\\\":3}\",foo))";
            Assert.Equal(expected, text);
        }

        [Fact]
        public void It_can_pass_through_multiple_arguments()
        {
            var argument = new { foo = 3 };
            var secondArgument = new { bar = 2 };

            var list = ArgumentListGenerator.GenerateArgumentListForGetProgramState(new FilePosition
            {
                Line = 1,
                Character = 1,
                File = "test.cs"
            },(argument, "foo"), (secondArgument, "bar"));

            var text = list.ToString();
            var expected = "(\"{\\\"line\\\":1,\\\"character\\\":1,\\\"file\\\":\\\"test.cs\\\"}\",(\"{\\\"foo\\\":3}\",foo),(\"{\\\"bar\\\":2}\",bar))";
            Assert.Equal(expected, text);
        }

    }
}
