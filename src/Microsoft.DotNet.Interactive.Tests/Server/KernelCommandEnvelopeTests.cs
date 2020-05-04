// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Server;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Server
{
    public class KernelCommandEnvelopeTests
    {
        [Fact(Timeout = 45000)]
        public void Create_creates_envelope_of_the_correct_type()
        {
            IKernelCommand command = new SubmitCode("display(123)");

            var envelope = KernelCommandEnvelope.Create(command);

            envelope.Should().BeOfType<KernelCommandEnvelope<SubmitCode>>();
        }
        
        [Fact(Timeout = 45000)]
        public void Create_creates_envelope_with_reference_to_original_command()
        {
            IKernelCommand command = new SubmitCode("display(123)");

            var envelope = KernelCommandEnvelope.Create(command);

            envelope.Command.Should().BeSameAs(command);
        }
    }
}