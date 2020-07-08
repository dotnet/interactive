// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        private static CompositeKernel CreateKernel() =>
            new CompositeKernel
            {
                new KeyValueStoreKernel(),
                new FakeKernel()
            };
    }
}