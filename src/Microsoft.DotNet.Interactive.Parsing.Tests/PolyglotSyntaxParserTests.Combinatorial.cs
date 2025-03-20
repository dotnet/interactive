// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Parsing.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Parsing.Tests;

public partial class PolyglotSyntaxParserTests
{
    [TestClass]
    public class Combinatorial
    {
        private readonly TestContext _output;

        public Combinatorial(TestContext output)
        {
            _output = output;
        }

        [TestMethod]
        [DynamicData(nameof(GenerateValidDirectives))]
        public void Valid_syntax_produces_expected_parse_tree_and_no_diagnostics(ISyntaxSpec syntaxSpec, int generation)
        {
            var code = syntaxSpec.ToString();

            var syntaxTree = Parse(code);

            _output.WriteLine($"""
                               === Generation #{generation} ===

                               {code}
                               """);

            syntaxTree.RootNode.GetDiagnostics().Should().BeEmpty();

            syntaxSpec.Validate(syntaxTree.RootNode);
        }

        [TestMethod]
        [DynamicData(nameof(GenerateValidDirectivesWithNonDirectiveCode))]
        public void Valid_syntax_with_extra_trivia_and_non_directive_code_produces_expected_parse_tree_and_no_diagnostics(ISyntaxSpec syntaxSpec, int generation)
        {
            var code = syntaxSpec.ToString();

            var syntaxTree = Parse(code);

            _output.WriteLine($"""
                               === Generation #{generation} ===

                               {code}
                               """);

            syntaxTree.RootNode.GetDiagnostics().Should().BeEmpty();

            syntaxSpec.Validate(syntaxTree.RootNode);
        }

        [TestMethod]
        [DynamicData(nameof(GenerateInvalidDirectives))]
        public void Invalid_syntax_produces_diagnostics(ISyntaxSpec syntaxSpec, int generation)
        {
            var code = syntaxSpec.ToString();

            var syntaxTree = Parse(code);

            _output.WriteLine($"""
                               === Generation #{generation} ===

                               {code}
                               """);

            syntaxTree.RootNode.GetDiagnostics().Should().NotBeEmpty();

            syntaxSpec.Validate(syntaxTree.RootNode);
        }

        [TestMethod]
        [DynamicData(nameof(GenerateValidDirectivesWithNonDirectiveCode))]
        public void Code_that_a_user_has_not_finished_typing_round_trips_correctly_and_does_not_throw(ISyntaxSpec syntaxSpec, int generation)
        {
            var code = syntaxSpec.ToString();

            for (var truncateAfter = 0; truncateAfter < code.Length; truncateAfter++)
            {
                var truncatedCode = code[..truncateAfter];

                _output.WriteLine($"""
                                   === Generation #{generation} truncated after {truncateAfter} characters ===

                                   {truncatedCode}
                                   """);

                Parse(truncatedCode);
            }
        }

        public static IEnumerable<object[]> GenerateValidDirectives()
        {
            var generationNumber = 0;

            foreach (var directive in ValidDirectives())
            {
                ++generationNumber;
                yield return
                [
                    new PolyglotSubmissionSyntaxSpec(directive),
                    generationNumber
                ];
            }
        }

        private static IEnumerable<DirectiveSyntaxSpec> ValidDirectives()
        {
            yield return new DirectiveSyntaxSpec(
                """
                #!set --name theVariable --value 123
                """);

            yield return new DirectiveSyntaxSpec(
                """
                #!set --name something --value @fsharp:theVariable
                """);

            yield return new DirectiveSyntaxSpec(
                """
                #!connect mssql --kernel-name adventureworks @input:{ "prompt": "Please provide a connection string", "save": true, "type": "file" }
                """);
        }

        public static IEnumerable<object[]> GenerateValidDirectivesWithNonDirectiveCode()
        {
            var generationNumber = 0;

            foreach (var directive in ValidDirectives())
            {
                ++generationNumber;

                yield return
                [
                    new PolyglotSubmissionSyntaxSpec(
                        new DirectiveSyntaxSpec(
                            """
                            #!csharp
                            """),
                        new LanguageNodeSpec(
                            """
                            var x = 123;
                            """,
                            expectedTargetKernelName: "csharp"),
                        directive)
                    {
                        Randomizer = new Random(1)
                    },
                    generationNumber
                ];

                yield return
                [
                    new PolyglotSubmissionSyntaxSpec(
                        directive,
                        new DirectiveSyntaxSpec(
                            """
                            #!csharp
                            """),
                        new LanguageNodeSpec(
                            """
                            Console.WriteLine("Hello");
                            """,
                            expectedTargetKernelName: "csharp"))
                    {
                        Randomizer = new Random(1)
                    },
                    generationNumber
                ];

                yield return
                [
                    new PolyglotSubmissionSyntaxSpec(
                        new DirectiveSyntaxSpec(
                            """
                            #!csharp
                            """),
                        directive,
                        new LanguageNodeSpec(
                            """
                            Console.WriteLine("Hello");
                            """,
                            expectedTargetKernelName: "csharp"))
                    {
                        Randomizer = new Random(1)
                    },
                    generationNumber
                ];
            }
        }

        public static IEnumerable<object[]> GenerateInvalidDirectives()
        {
            var generationNumber = 0;

            foreach (var directive in InvalidDirectives())
            {
                ++generationNumber;

                yield return
                [
                    new PolyglotSubmissionSyntaxSpec(
                        new DirectiveSyntaxSpec("#!csharp"),
                        new LanguageNodeSpec("""
                                             var x = 123;
                                             """,
                                             expectedTargetKernelName: "csharp"),
                        directive),
                    generationNumber
                ];

                yield return
                [
                    new PolyglotSubmissionSyntaxSpec(
                        directive,
                        new DirectiveSyntaxSpec("#!csharp"),
                        new LanguageNodeSpec("""
                                             Console.WriteLine("Hello");
                                             """,
                                             expectedTargetKernelName: "csharp")),
                    generationNumber
                ];
            }
        }

        private static IEnumerable<DirectiveSyntaxSpec> InvalidDirectives()
        {
            yield return new DirectiveSyntaxSpec(
                """
                #!set --value 123
                """);

            yield return new DirectiveSyntaxSpec(
                """
                #!set --name theVariable --value
                """);

            yield return new DirectiveSyntaxSpec(
                """
                #!set --name something --value [ 1, 2, ]
                """);
        }
    }
}