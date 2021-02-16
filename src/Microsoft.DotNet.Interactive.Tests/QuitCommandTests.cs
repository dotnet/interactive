// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class QuitCommandTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        public QuitCommandTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact]
        public async Task quit_command_fails_when_not_configured()
        {

            var subKernel = new CSharpKernel();

            using var compositeKernel = new CompositeKernel
            {
                subKernel
            };

            compositeKernel.DefaultKernelName = subKernel.Name;



            var quitCommand = new Quit();

            var events = compositeKernel.KernelEvents.ToSubscribedList();

            await compositeKernel.SendAsync(quitCommand);

            using var _ = new AssertionScope();

            events
                .Should().ContainSingle<CommandFailed>()
                .Which
                .Command
                .Should()
                .Be(quitCommand);

            events
                .Should().ContainSingle<CommandFailed>()
                .Which
                .Exception
                .Should()
                .BeOfType<InvalidOperationException>();
        }
    }
}