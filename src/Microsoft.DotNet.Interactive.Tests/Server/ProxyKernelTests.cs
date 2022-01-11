// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Connection;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests.Server
{
    public class ProxyKernelTests
    {
        [Fact]
        public void cannot_be_started_multiple_times()
        {
            using var kernel = new ProxyKernel("proxy", new BlockingCommandAndEventReceiver(),
                new RecordingKernelCommandAndEventSender());

            kernel.StartAsync();

            var restart = new Action(() => kernel.StartAsync());

            restart.Should().Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be("ProxyKernel proxy is already started.");
        }
    }
}