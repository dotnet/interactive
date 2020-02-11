// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using RxClockExtension;
using Xunit;

namespace SampleExtensions.Tests
{
    public class RxClockKernelExtensionTests : IDisposable
    {
        private readonly IKernel _kernel;

        public RxClockKernelExtensionTests()
        {
            _kernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            Task.Run(() => new RxClockKernelExtension().OnLoadAsync(_kernel))
                .Wait();

            KernelEvents = _kernel.KernelEvents.ToSubscribedList();
        }

        public SubscribedList<IKernelEvent> KernelEvents { get; set; }

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
        public async Task It_formats_any_IObservable_DateTime()
        {
            await _kernel.SubmitCodeAsync(@"
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

Observable
    .Timer(TimeSpan.FromSeconds(1), Scheduler.CurrentThread)
    .Repeat()
    .Take(10)
");

            AssertThatClockWasRendered();
        }

        private void AssertThatClockWasRendered()
        {
            KernelEvents
                .Should()
                .ContainSingle<DisplayEventBase>()
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