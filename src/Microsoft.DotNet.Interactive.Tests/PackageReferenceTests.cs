// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class PackageReferenceTests
    {
        [Theory]
        [InlineData("nuget:PocketLogger")]
        [InlineData("nuget:PocketLogger,1.2.3")]
        [InlineData("nuget:PocketLogger, 1.2.3")]
        [InlineData("nuget:PocketLogger, 1.2.3-beta")]
        public void Nuget_package_reference_correctly_parses_package_name(string value)
        {
            PackageReference.TryParse(value, out var reference);

            reference.PackageName.Should().Be("PocketLogger");
        }

        [Theory]
        [InlineData("nuget:PocketLogger", "")]
        [InlineData("nuget:PocketLogger,1.2.3", "1.2.3")]
        [InlineData("nuget:PocketLogger, 1.2.3", "1.2.3")]
        [InlineData("nuget:PocketLogger, 1.2.3-beta", "1.2.3-beta")]
        public void Nuget_package_reference_correctly_parses_package_version(string value, string expectedVersion)
        {
            PackageReference.TryParse(value, out var reference);

            reference.PackageVersion.Should().Be(expectedVersion);
        }
    }
}