﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Server;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Connection;

public class KernelCommandAndEventTextStreamSenderTests
{
    [Fact]
    public async Task when_a_KernelEvent_is_sent_it_writes_a_KernelEventEnvelope()
    {
        var kernelEvent = new CommandSucceeded(new SubmitCode("x=1"));
        var buffer = new StringBuilder();

        var sender = new KernelCommandAndEventTextStreamSender(new StringWriter(buffer), KernelHost.CreateHostUri("test"));
        await sender.SendAsync(kernelEvent,CancellationToken.None);

        var envelopeMessage = buffer.ToString();

        envelopeMessage.Should()
                       .BeEquivalentTo(KernelEventEnvelope.Serialize(KernelEventEnvelope.Create(kernelEvent)) + KernelCommandAndEventTextStreamSender.Delimiter);
    }

    [Fact]
    public async Task when_a_KernelCommand_is_sent_it_writes_a_KernelCommandEnvelope()
    {
        var kernelCommand = new SubmitCode("x=1");
        var buffer = new StringBuilder();

        var sender = new KernelCommandAndEventTextStreamSender(new StringWriter(buffer), KernelHost.CreateHostUri("test"));
        await sender.SendAsync(kernelCommand,CancellationToken.None);

        var envelopeMessage = buffer.ToString();

        envelopeMessage.Should()
                       .BeEquivalentTo(KernelCommandEnvelope.Serialize(KernelCommandEnvelope.Create(kernelCommand)) + KernelCommandAndEventTextStreamSender.Delimiter);
    }
}