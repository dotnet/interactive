// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
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
                                   .UseDefaultMagicCommands()
                                   .LogEventsToPocketLogger();

                using var events = kernel.KernelEvents.ToSubscribedList();

                await kernel.SendAsync(new SubmitCode(
                                           @"
%%time

using System.Threading.Tasks;
await Task.Delay(500);
display(""done!"");
"));

                events.Should()
                      .ContainSingle(e => e is DisplayedValueProduced &&
                                          e.As<DisplayedValueProduced>().Value is TimeSpan)
                      .Which
                      .As<DisplayedValueProduced>()
                      .FormattedValues
                      .Should()
                      .ContainSingle(v =>
                                         v.MimeType == "text/plain" &&
                                         v.Value.ToString().StartsWith("Wall time:") &&
                                         v.Value.ToString().EndsWith("ms"));

                events.OfType<DisplayedValueProduced>()
                      .SelectMany(e => e.FormattedValues)
                      .Should()
                      .Contain(v => v.Value.Equals("done!"));
            }
        }
    }
}