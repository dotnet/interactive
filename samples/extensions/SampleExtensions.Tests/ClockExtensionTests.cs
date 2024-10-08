// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using ClockExtension;
using FluentAssertions;
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
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

            ClockKernelExtension.Load(_kernel);
        }

        public void Dispose()
        {
            _kernel.Dispose();
        }

        [Fact]
        public async Task It_formats_DateTime()
        {
            var result = await _kernel.SubmitCodeAsync("DateTime.Now");

            AssertThatClockWasRendered(result);
        }

        [Fact]
        public async Task It_formats_DateTimeOffset()
        {
            var result = await _kernel.SubmitCodeAsync("DateTimeOffset.Now");

            AssertThatClockWasRendered(result);
        }

        [Fact]
        public async Task It_adds_a_clock_magic_command()
        {
            var result = await _kernel.SubmitCodeAsync("#!clock");

            AssertThatClockWasRendered(result);
        }

        private void AssertThatClockWasRendered(KernelCommandResult result) =>
            result.Events
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