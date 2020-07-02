// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharp.Tests
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
    }
}