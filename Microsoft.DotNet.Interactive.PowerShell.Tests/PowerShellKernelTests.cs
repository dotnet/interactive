// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.PowerShell.Tests
{
    public class PowerShellKernelTests
    {
        [Theory]
        [InlineData(@"$x = New-Object -TypeName System.IO.FileInfo -ArgumentList c:\temp\some.txt", typeof(FileInfo))]
        [InlineData("$x = \"hello!\"", typeof(string))]
        public async Task TryGetVariable_unwraps_PowerShell_object(string code, Type expectedType)
        {
            using var kernel = new PowerShellKernel();

            await kernel.SubmitCodeAsync(code);

            kernel.TryGetVariable("x", out var fi).Should().BeTrue();

            fi.Should().BeOfType(expectedType);
        }
    }
}