// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;

using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Server
{
    public class KernelCommandAndEventTextStreamReceiverTests
    {
        [Fact]
        public async Task when_a_KernelEventEnvelope_is_received_it_reads_the_event()
        {
            var kernelEvent = new CommandSucceeded(new SubmitCode("x=1"));
            var message = KernelEventEnvelope.Serialize( KernelEventEnvelope.Create(kernelEvent));

            using var stringReader = new StringReader(message);
            var receiver = new KernelCommandAndEventTextStreamReceiver(stringReader);

            var d = await receiver.CommandsOrEventsAsync().FirstAsync();

            d.Event.Should().BeEquivalentTo(kernelEvent);
        }

        [Fact]
        public async Task when_a_KernelCommandEnvelope_is_received_it_reads_the_command()
        {
            var kernelCommand = new SubmitCode("x=1");
            var message = KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand));

            using var stringReader = new StringReader(message);
            var receiver = new KernelCommandAndEventTextStreamReceiver(stringReader);

            var d = await receiver.CommandsOrEventsAsync().FirstAsync();

            d.Command.Should().BeEquivalentTo(kernelCommand);
        }
    }

    public class KernelCommandAndEventTextStreamSenderTests
    {
        [Fact]
        public async Task when_a_KernelEvent_is_sent_it_sends_a_KernelEventEnvelope()
        {
            var kernelEvent = new CommandSucceeded(new SubmitCode("x=1"));
            var buffer = new StringBuilder();

            var sender = new KernelCommandAndEventTextStreamSender(new StringWriter(buffer));
            await sender.SendAsync(kernelEvent);

            var envelopeMessage = buffer.ToString();

            envelopeMessage.Should()
                .BeEquivalentTo(KernelEventEnvelope.Serialize(KernelEventEnvelope.Create(kernelEvent)) + KernelCommandAndEventTextStreamSender.Delimiter);
        }

        [Fact]
        public async Task when_a_KernelCommand_is_sent_it_sends_a_KernelCommandEnvelope()
        {
            var kernelCommand = new SubmitCode("x=1");
            var buffer = new StringBuilder();

            var sender = new KernelCommandAndEventTextStreamSender(new StringWriter(buffer));
            await sender.SendAsync(kernelCommand);

            var envelopeMessage = buffer.ToString();

            envelopeMessage.Should()
                .BeEquivalentTo(KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand)) + KernelCommandAndEventTextStreamSender.Delimiter);
        }
    }
}
