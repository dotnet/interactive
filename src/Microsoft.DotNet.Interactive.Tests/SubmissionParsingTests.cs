// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class SubmissionParsingTests
    {
        [Theory]
        [InlineData(@"
#r one.dll
#r two.dll", "csharp")]
        [InlineData(@"
#r one.dll
var x = 123; // with some intervening code
#r two.dll", "csharp")]
        [InlineData(@"
#r one.dll
#r two.dll", "fsharp")]
        [InlineData(@"
#r one.dll
let x = 123 // with some intervening code
#r two.dll", "fsharp")]
        public void Multiple_pound_r_directives_are_submitted_together(
            string code,
            string defaultKernel)
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
                new FSharpKernel().UseNugetDirective(),
            };

            kernel.DefaultKernelName = defaultKernel;

            var commands = kernel.SubmissionParser.SplitSubmission(new SubmitCode(code));

            commands
                .Should()
                .ContainSingle<SubmitCode>()
                .Which
                .Code
                .Should()
                .ContainAll("#r one.dll", "#r two.dll");
        }

        [Fact]
        public void RequestDiagnostics_can_be_split_into_separate_commands()
        {
            var markupCode = @"

#!time$$

// language-specific code";

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var startLineOfCode, out var _column);

            var sourceText = SourceText.From(code);

            var command = new RequestDiagnostics(code);
            var commands = new CSharpKernel().SubmissionParser.SplitSubmission(command);

            int LineFromPosition(int position)
            {
                var linePosition = sourceText.Lines.GetLinePosition(position);
                return linePosition.Line;
            }

            commands
                .Should()
                .ContainSingle<RequestDiagnostics>(d => LineFromPosition(d.LanguageNode.Span.Start) == startLineOfCode);
        }
    }
}