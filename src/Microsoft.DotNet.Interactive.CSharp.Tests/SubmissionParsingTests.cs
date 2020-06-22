﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharp.Tests
{
    public class SubmissionParsingTests
    {
        [Fact]
        public async Task pound_r_is_split_into_separate_command_from_csharp_code()
        {
            var receivedCommands = new List<KernelCommand>();

            using var kernel = new CSharpKernel();

            kernel.AddMiddleware((command, context, next) =>
            {
                receivedCommands.Add(command);
                return Task.CompletedTask;
            });

            kernel.UseNugetDirective();
            var path = Path.GetTempFileName();
            var poundR = $@"#r ""{path}""";
            var usingStatement = "using Some.Namespace;";
            var nextSubmission = "// the code";

            kernel.DeferCommand(new SubmitCode(poundR + Environment.NewLine + usingStatement));

            await kernel.SubmitCodeAsync(nextSubmission);

            receivedCommands
                .Cast<SubmitCode>()
                .Select(c => c.Code.Trim())
                .Should()
                .BeEquivalentSequenceTo(
                    poundR,
                    usingStatement,
                    nextSubmission);
        }
    }
}