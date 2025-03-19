// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Tests;

public partial class KernelTests
{
    [TestClass]
    public class RegisteringCommandHandlers
    {
        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void When_command_type_registered_then_kernel_registers_envelope_type_for_serialization(bool withHandler)
        {
            KernelCommandEnvelope.RegisterDefaults();

            using var kernel = new FakeKernel();

            if (withHandler)
            {
                kernel.RegisterCommandHandler<CustomCommandTypes.FirstSubmission.MyCommand>(
                    (_, _) => Task.CompletedTask);
            }
            else
            {
                kernel.RegisterCommandType<CustomCommandTypes.FirstSubmission.MyCommand>();
            }

            var originalCommand = new CustomCommandTypes.FirstSubmission.MyCommand("xyzzy");
            string envelopeJson = KernelCommandEnvelope.Serialize(originalCommand);
            var roundTrippedCommandEnvelope = KernelCommandEnvelope.Deserialize(envelopeJson);

            roundTrippedCommandEnvelope
                .Command
                .Should()
                .BeOfType<CustomCommandTypes.FirstSubmission.MyCommand>()
                .Which
                .Info
                .Should()
                .Be(originalCommand.Info);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void When_command_type_reregistered_with_changed_type_command_then_kernel_registers_updated_envelope_type_for_serialization(bool withHandler)
        {
            // Notebook authors should be able to develop their custom commands experimentally and progressively,
            // so we don't want any "you have to restart your kernel now" situations just because you already
            // called RegisterCommandHandler once for a particular command type.
            KernelCommandEnvelope.RegisterDefaults();

            using var kernel = new FakeKernel();

            if (withHandler)
            {
                kernel.RegisterCommandHandler<CustomCommandTypes.FirstSubmission.MyCommand>(
                    (_, _) => Task.CompletedTask);
                kernel.RegisterCommandHandler<CustomCommandTypes.SecondSubmission.MyCommand>(
                    (_, _) => Task.CompletedTask);
            }
            else
            {
                kernel.RegisterCommandType<CustomCommandTypes.FirstSubmission.MyCommand>();
                kernel.RegisterCommandType<CustomCommandTypes.SecondSubmission.MyCommand>();
            }

            var originalCommand = new CustomCommandTypes.SecondSubmission.MyCommand("xyzzy", 42);
            string envelopeJson = KernelCommandEnvelope.Serialize(originalCommand);
            var roundTrippedCommandEnvelope = KernelCommandEnvelope.Deserialize(envelopeJson);

            roundTrippedCommandEnvelope
                .Command
                .Should()
                .BeOfType<CustomCommandTypes.SecondSubmission.MyCommand>()
                .Which
                .Info
                .Should()
                .Be(originalCommand.Info);
            roundTrippedCommandEnvelope
                .Command
                .As<CustomCommandTypes.SecondSubmission.MyCommand>()
                .AdditionalProperty
                .Should()
                .Be(originalCommand.AdditionalProperty);
        }
    }
}
