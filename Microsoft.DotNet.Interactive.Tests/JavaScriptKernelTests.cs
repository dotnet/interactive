// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Parsing;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class JavaScriptKernelTests
    {
        [Fact]
        public async Task javascript_kernel_emits_code_as_it_was_given()
        {
            using var kernel = new CompositeKernel
            {
                new JavaScriptKernel()
            };

            var scriptContent = "alert('Hello World!');";

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode($"#!javascript\n{scriptContent}"));

            var formatted =
                events
                    .OfType<DisplayedValueProduced>()
                    .Select(v => v.Value)
                    .Cast<ScriptContent>()
                    .ToArray();

            if (SubmissionParser.USE_NEW_SUBMISSION_SPLITTER)
            {
                formatted
                    .Should()
                    .ContainSingle()
                    .Which
                    .ScriptValue
                    .Should()
                    .Be("\n" + scriptContent);
            }
            else
            {
                formatted
                    .Should()
                    .ContainSingle()
                    .Which
                    .ScriptValue
                    .Should()
                    .Be(scriptContent);
            }
        }
    }
}