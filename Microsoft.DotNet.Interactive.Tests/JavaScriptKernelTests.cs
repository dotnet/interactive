﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class JavaScriptKernelTests
    {
        [Fact]
        public async Task javascript_emits_string_as_content_within_a_script_element()
        {
            using var kernel = new CompositeKernel
            {
                new JavaScriptKernel()
            };

            var scriptContent = "alert('Hello World!');";

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode(
                                       $"#!javascript\n{scriptContent}"));

            var formatted =
                events
                    .OfType<DisplayedValueProduced>()
                    .SelectMany(v => v.FormattedValues)
                    .ToArray();

            formatted
                .Should()
                .ContainSingle(v => v.MimeType == "text/html")
                .Which
                .Value
                .Trim()
                .Should()
                .Contain($@"<script type=""text/javascript"">{scriptContent}</script>");
        }
    }
}