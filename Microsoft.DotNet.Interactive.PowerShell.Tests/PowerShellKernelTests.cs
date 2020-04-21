// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;
using FluentAssertions;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using XPlot.Plotly;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.PowerShell.Tests
{
    public class PowerShellKernelTests : LanguageKernelTestBase
    {
        private readonly string _allUsersCurrentHostProfilePath = Path.Combine(Path.GetDirectoryName(typeof(PSObject).Assembly.Location), "Microsoft.dotnet-interactive_profile.ps1");

        [Theory]
        [InlineData(@"$x = New-Object -TypeName System.IO.FileInfo -ArgumentList c:\temp\some.txt", typeof(FileInfo))]
        [InlineData("$x = \"hello!\"", typeof(string))]
        public async Task TryGetVariable_unwraps_PowerShell_object(string code, Type expectedType)
        {
            using var kernel = new PowerShellKernel();

            await kernel.SubmitCodeAsync(code);

            kernel.TryGetVariable("x", out object fi).Should().BeTrue();

            fi.Should().BeOfType(expectedType);
        }

        [Fact]
        public async Task GetCorrectProfilePaths()
        {
            using var kernel = new PowerShellKernel().UseProfiles();

            // Set variables we will retrieve later.
            await kernel.SubmitCodeAsync("$currentUserCurrentHost = $PROFILE.CurrentUserCurrentHost");
            await kernel.SubmitCodeAsync("$allUsersCurrentHost = $PROFILE.AllUsersCurrentHost");

            kernel.TryGetVariable("currentUserCurrentHost", out var profileObj).Should().BeTrue();
            profileObj.Should().BeOfType<string>();
            string currentUserCurrentHost = profileObj.As<string>();

            // Get $PROFILE default.
            kernel.TryGetVariable("PROFILE", out profileObj).Should().BeTrue();
            profileObj.Should().BeOfType<string>();
            string profileDefault = profileObj.As<string>();

            // Check that $PROFILE is not null or empty and it is the same as
            // $PROFILE.CurrentUserCurrentHost
            profileDefault.Should().NotBeNullOrEmpty();
            profileDefault.Should().Be(currentUserCurrentHost);

            kernel.TryGetVariable("allUsersCurrentHost", out profileObj).Should().BeTrue();
            profileObj.Should().BeOfType<string>();
            string allUsersCurrentHost = profileObj.As<string>();

            // Check that $PROFILE.AllUsersCurrentHost is what we expect it is:
            // $PSHOME + Microsoft.dotnet-interactive_profile.ps1
            allUsersCurrentHost.Should().Be(_allUsersCurrentHostProfilePath);
        }

        [Fact]
        public async Task VerifyAllUsersProfileRuns()
        {
            var randomVariableName = Path.GetRandomFileName().Split('.')[0];
            File.WriteAllText(_allUsersCurrentHostProfilePath, $"$global:{randomVariableName} = $true");

            try
            {
                using var kernel = new PowerShellKernel().UseProfiles();

                // trigger first time setup.
                await kernel.SubmitCodeAsync("Get-Date");

                kernel.TryGetVariable(randomVariableName, out var profileObj).Should().BeTrue();

                profileObj.Should().BeOfType<bool>();
                profileObj.As<bool>().Should().BeTrue();
            }
            finally
            {
                
                File.Delete(_allUsersCurrentHostProfilePath);
            }
        }
    }
}
