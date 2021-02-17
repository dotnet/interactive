// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class LogDirectiveTests
    {
        [Fact]
        public async Task It_outputs_diagnostic_log_events()
        {
            using var kernel = new CSharpKernel()
                .UseLogMagicCommand();

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync("#!log\n123");

            events.Should()
                  .ContainSingle<DiagnosticLogEntryProduced>(
                      e => e.Message == "Logging enabled");
        }
    }
}