// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using ClockExtension;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace SampleExtensions.Tests
{
    public class ClockExtensionTests : IDisposable
    {
        private readonly Kernel _kernel;

        public ClockExtensionTests()
        {
            _kernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            Task.Run(() => new ClockKernelExtension().OnLoadAsync(_kernel))
                .Wait();

            KernelEvents = _kernel.KernelEvents.ToSubscribedList();
        }

        public SubscribedList<KernelEvent> KernelEvents { get; }

        public void Dispose()
        {
            _kernel.Dispose();
            KernelEvents.Dispose();
        }

        [Fact]
        public async Task It_formats_DateTime()
        {
            using var events = _kernel.KernelEvents.ToSubscribedList();

            await _kernel.SubmitCodeAsync("DateTime.Now");

            AssertThatClockWasRendered();
        }

        [Fact]
        public async Task It_formats_DateTimeOffset()
        {
            using var events = _kernel.KernelEvents.ToSubscribedList();

            await _kernel.SubmitCodeAsync("DateTimeOffset.Now");

            AssertThatClockWasRendered();
        }

        [Fact]
        public async Task It_adds_a_clock_magic_command()
        {
            using var events = _kernel.KernelEvents.ToSubscribedList();

            await _kernel.SubmitCodeAsync("#!clock");

            AssertThatClockWasRendered();
        }

        private void AssertThatClockWasRendered()
        {
            KernelEvents
                .Should()
                .ContainSingle<DisplayEvent>()
                .Which
                .FormattedValues
                .Should()
                .ContainSingle(v => v.MimeType == "text/html")
                .Which
                .Value
                .Should()
                .Contain("<circle");
        }
    }
}