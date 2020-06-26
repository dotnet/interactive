// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Linq;

using FluentAssertions;

using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Jupyter;

using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class ConnectTests
    {
        [Fact]
        public void connect_command_is_not_available_by_default()
        {
            using var compositeKernel = new CompositeKernel
            {
                new CSharpKernel().UseDefaultMagicCommands()
            };

            compositeKernel.Directives
                .Should()
                .NotContain(c => c.Name == "#!connect");
        }

        [Fact]
        public void connect_command_is_available_when_a_user_adds_a_kernel_connection_type()
        {
            using var compositeKernel = new CompositeKernel
            {
                new CSharpKernel().UseDefaultMagicCommands()
            };

            compositeKernel.AddConnectionDirective(
                new Command("Data", "Connects to a data kernel")
            );

            compositeKernel.Directives
                .Should()
                .Contain(c => c.Name == "#!connect");
        }

        [Fact]
        public void when_a_user_defines_kernel_connection_type_it_is_available_as_subcommand_of_connect()
        {
            using var compositeKernel = new CompositeKernel
            {
                new CSharpKernel().UseDefaultMagicCommands()
            };

            compositeKernel.AddConnectionDirective(
                new Command("Data", "Connects to a data kernel")
            );

            compositeKernel.Directives
                .Should()
                .ContainSingle(c => c.Name == "#!connect")
                .Which
                .Children
                .OfType<ICommand>()
                .Should()
                .ContainSingle(c => c.Name == "Data");
        }
    }
}
