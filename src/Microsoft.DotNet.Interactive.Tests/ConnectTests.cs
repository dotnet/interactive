// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Jupyter;
using Microsoft.DotNet.Interactive.Tests.Utility;

using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class ConnectTests
    {
        [Fact]
        public async Task connect_command_is_not_available_by_default()
        {
            using var compositeKernel = new CompositeKernel
            {
                new CSharpKernel().UseDefaultMagicCommands()
            };

            var events = compositeKernel.KernelEvents.ToSubscribedList();

            await compositeKernel.SendAsync(new SubmitCode("#!lsmagic"));

            var valueProducedEvents = events.OfType<DisplayedValueProduced>().ToArray();

            valueProducedEvents[0].FormattedValues
                .FirstOrDefault(fv => fv.MimeType == HtmlFormatter.MimeType)
                .Value
                .Should()
                .NotContain("#!connect");
        }

        [Fact]
        public async Task connect_command_is_available_when_subcommands_are_added()
        {
            using var compositeKernel = new CompositeKernel
            {
                new CSharpKernel().UseDefaultMagicCommands()
            };

            compositeKernel.ConfigureConnection(
                new Command("customTransport", "Connects to remote kernel via custom Transport")
            );

            var events = compositeKernel.KernelEvents.ToSubscribedList();

            await compositeKernel.SendAsync(new SubmitCode("#!lsmagic"));

            var valueProducedEvents = events.OfType<DisplayedValueProduced>().ToArray();

            valueProducedEvents[0].FormattedValues
                .FirstOrDefault(fv => fv.MimeType == HtmlFormatter.MimeType)
                .Value
                .Should()
                .ContainAll("#!connect", "customTransport");
        }
    }
}
