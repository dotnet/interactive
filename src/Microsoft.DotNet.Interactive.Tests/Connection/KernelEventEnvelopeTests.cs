// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Connection;

namespace Microsoft.DotNet.Interactive.Tests.Connection;

[TestClass]
public class KernelEventEnvelopeTests
{
    [TestMethod]
    public void Create_creates_envelope_of_the_correct_type()
    {
        KernelEvent @event = new DisplayedValueProduced(
            123,
            new SubmitCode("display(123)"));

        var envelope = KernelEventEnvelope.Create(@event);

        envelope.Should().BeOfType<KernelEventEnvelope<DisplayedValueProduced>>();
    }
        
    [TestMethod]
    public void Create_creates_envelope_with_reference_to_original_event()
    {
        KernelEvent @event = new DisplayedValueProduced(
            123,
            new SubmitCode("display(123)"));

        var envelope = KernelEventEnvelope.Create(@event);

        envelope.Event.Should().BeSameAs(@event);
    }
}