// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Telemetry.Tests
{
    public class TelemetryCommonPropertiesTests
    {
        [Fact]
        public void Telemetry_common_properties_should_contain_if_it_is_in_docker_or_not()
        {
            var unitUnderTest = new TelemetryCommonProperties("product-version", userLevelCacheWriter: new NothingUserLevelCacheWriter());
            unitUnderTest.GetTelemetryCommonProperties().Should().ContainKey("Docker Container");
        }

        [Fact]
        public void Telemetry_common_properties_should_return_hashed_machine_id()
        {
            var unitUnderTest = new TelemetryCommonProperties("product-version", getMACAddress: () => "plaintext", userLevelCacheWriter: new NothingUserLevelCacheWriter());
            unitUnderTest.GetTelemetryCommonProperties()["Machine ID"].Should().NotBe("plaintext");
        }

        [Fact]
        public void Telemetry_common_properties_should_return_new_guid_when_cannot_get_mac_address()
        {
            var unitUnderTest = new TelemetryCommonProperties("product-version", getMACAddress: () => null, userLevelCacheWriter: new NothingUserLevelCacheWriter());
            var assignedMachineId = unitUnderTest.GetTelemetryCommonProperties()["Machine ID"];

            Guid.TryParse(assignedMachineId, out var _).Should().BeTrue("it should be a guid");
        }

        [Fact]
        public void Telemetry_common_properties_should_contain_kernel_version()
        {
            var unitUnderTest = new TelemetryCommonProperties("product-version", getMACAddress: () => null, userLevelCacheWriter: new NothingUserLevelCacheWriter());
            unitUnderTest.GetTelemetryCommonProperties()["Kernel Version"].Should().Be(RuntimeInformation.OSDescription);
        }
    }
}