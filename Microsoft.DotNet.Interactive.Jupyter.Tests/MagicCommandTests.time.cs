// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using System.Threading.Tasks;
using FluentAssertions.Extensions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public partial class MagicCommandTests
    {
        public class time
        {
            [Fact]
            public async Task time_produces_time_elapsed_to_run_the_code_submission()
            {
                using var kernel = new CompositeKernel
                                   {
                                       new CSharpKernel().UseKernelHelpers()
                                   }
                                   .UseDefaultMagicCommands();

                using var events = kernel.KernelEvents.ToSubscribedList();

                await kernel.SendAsync(new SubmitCode(
                                           @"
#!time

using System.Threading.Tasks;
await Task.Delay(500);
display(123);
"));

                events.Should()
                      .ContainSingle<DisplayedValueProduced>(
                          e => e.As<DisplayedValueProduced>().Value is TimeSpan)
                      .Which
                      .FormattedValues
                      .Should()
                      .ContainSingle(v =>
                                         v.MimeType == "text/plain" &&
                                         v.Value.ToString().StartsWith("Wall time:") &&
                                         v.Value.ToString().EndsWith("ms"));

                events.Should()
                      .ContainSingle<DisplayedValueProduced>(
                          e => e.As<DisplayedValueProduced>().Value is int)
                      .Which
                      .FormattedValues
                      .Should()
                      .ContainSingle(v =>
                                         v.MimeType == "text/html" &&
                                         v.Value.ToString() == "123");

                events.Should()
                      .ContainSingle<DisplayedValueProduced>(
                          e => e.As<DisplayedValueProduced>().Value is TimeSpan)
                      .Which
                      .Value
                      .As<TimeSpan>()
                      .Should()
                      .BeGreaterOrEqualTo(500.Milliseconds());
            }
        }
    }
}