// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KeyValueStoreKernelTests
    {
        [Fact]
        public async Task SubmitCode_is_not_valid_without_a_value_name()
        {
            using var kernel = CreateKernel();

            var storedValue = "1,2,3";

            var result = await kernel.SubmitCodeAsync(
                             @$"
#!value
{storedValue}
");

            using var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<CommandFailed>()
                  .Which
                  .Message
                  .Should()
                  .Be("Option '--name' is required.");
        }

        [Fact]
        public async Task SubmitCode_stores_code_as_a_string()
        {
            using var kernel = CreateKernel();

            var storedValue = "1,2,3";

            await kernel.SubmitCodeAsync(
                @$"
#!value --name hi
{storedValue}");

            var keyValueStoreKernel = (DotNetKernel) kernel.FindKernel("value");

            keyValueStoreKernel.TryGetVariable("hi", out object retrievedValue);

            retrievedValue
                .Should()
                .BeOfType<string>()
                .Which
                .Should()
                .Be(storedValue);
        }

        [Fact]
        public async Task When_mime_type_is_specified_then_the_value_is_displayed_using_the_specified_mime_type()
        {
            using var kernel = CreateKernel();

            var storedValue = "1,2,3";

            var result = await kernel.SubmitCodeAsync(
                             @$"
#!value --name hello --mime-type text/test-stuff
{storedValue}
");

            using var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<DisplayedValueProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle(v => v.MimeType == "text/test-stuff" &&
                                      v.Value == storedValue);
        }

        private static CompositeKernel CreateKernel() =>
            new CompositeKernel
            {
                new KeyValueStoreKernel(),
                new FakeKernel()
            };
    }
}