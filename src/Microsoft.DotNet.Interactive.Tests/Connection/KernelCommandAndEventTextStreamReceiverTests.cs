// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Connection
{
    public class KernelCommandAndEventTextStreamReceiverTests
    {
        [Fact]
        public async Task when_a_KernelEventEnvelope_is_received_it_publishes_the_event()
        {
            var kernelEvent = new CommandSucceeded(new SubmitCode("x=1"));
            var message = KernelEventEnvelope.Serialize( KernelEventEnvelope.Create(kernelEvent));

            using var stringReader = new StringReader(message);
            var receiver = new KernelCommandAndEventTextReaderReceiver(stringReader);

            var d = await receiver.CommandsAndEventsAsync(CancellationToken.None).FirstAsync();

            d.Event.Should().BeEquivalentTo(kernelEvent);
        }

        [Fact]
        public async Task when_a_KernelCommandEnvelope_is_received_it_reads_the_command()
        {
            var kernelCommand = new SubmitCode("x=1");
            var message = KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand));

            using var stringReader = new StringReader(message);
            var receiver = new KernelCommandAndEventTextReaderReceiver(stringReader);

            var d = await receiver.CommandsAndEventsAsync(CancellationToken.None).FirstAsync();

            d.Command.Should().BeEquivalentTo(kernelCommand);
        }

        [Fact]
        public async Task when_invalid_json_is_received_it_produces_DiagnosticLogEntryProduced()
        {
            var invalidJson = " { hello";
            using var stringReader = new StringReader(invalidJson);
            var receiver = new KernelCommandAndEventTextReaderReceiver(stringReader);

            var d = await receiver.CommandsAndEventsAsync(CancellationToken.None).FirstAsync();

            d.Event.Should().BeOfType<DiagnosticLogEntryProduced>()
                .Which
                .Message
                .Should()
                .Contain(invalidJson); ;
        }
    }
}
