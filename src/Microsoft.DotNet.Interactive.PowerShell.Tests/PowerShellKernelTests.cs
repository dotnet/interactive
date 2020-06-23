// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Management.Automation;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using System.Linq;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using XPlot.Plotly;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.PowerShell.Tests
{
    public class PowerShellKernelTests : LanguageKernelTestBase
    {
        private readonly string _allUsersCurrentHostProfilePath = Path.Combine(Path.GetDirectoryName(typeof(PSObject).Assembly.Location), "Microsoft.dotnet-interactive_profile.ps1");

        public PowerShellKernelTests(ITestOutputHelper output) : base(output)
        {
        }

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
        public async Task PowerShell_progress_sends_updated_display_values()
        {
            var kernel = CreateKernel(Language.PowerShell);
            var command = new SubmitCode(@"
for ($j = 0; $j -le 4; $j += 4 ) {
    $p = $j * 25
    Write-Progress -Id 1 -Activity 'Search in Progress' -Status ""$p% Complete"" -PercentComplete $p
    Start-Sleep -Milliseconds 300
}
");
            var result = await kernel.SendAsync(command);

            var events = result.KernelEvents.ToSubscribedList();

            Assert.Collection(events,
                                       e => e.Should().BeOfType<CodeSubmissionReceived>(),
                                       e => e.Should().BeOfType<CompleteCodeSubmissionReceived>(),
                                       e => e.Should().BeOfType<DisplayedValueProduced>().Which
                                             .Value.Should().BeOfType<string>().Which
                                             .Should().Match("* Search in Progress* 0% Complete* [ * ] *"),
                                       e => e.Should().BeOfType<DisplayedValueUpdated>().Which
                                             .Value.Should().BeOfType<string>().Which
                                             .Should().Match("* Search in Progress* 100% Complete* [ooo*ooo] *"),
                                       e => e.Should().BeOfType<DisplayedValueUpdated>().Which
                                             .Value.Should().BeOfType<string>().Which
                                             .Should().Be(string.Empty),
                                       e => e.Should().BeOfType<CommandSucceeded>());
        }

        [Fact]
        public void PowerShell_type_accelerators_present()
        {
            CreateKernel(Language.PowerShell);

            var accelerator = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
            dynamic typeAccelerators = accelerator.GetProperty("Get").GetValue(null);
            Assert.Equal(typeAccelerators["Graph.Scatter"].FullName, $"{typeof(Graph).FullName}+Scatter");
            Assert.Equal(typeAccelerators["Layout"].FullName, $"{typeof(Layout).FullName}+Layout");
            Assert.Equal(typeAccelerators["Chart"].FullName, typeof(Chart).FullName);
        }

        [Fact]
        public async Task PowerShell_token_variables_work()
        {
            var kernel = CreateKernel(Language.PowerShell);

            await kernel.SendAsync(new SubmitCode("echo /this/is/a/path"));
            await kernel.SendAsync(new SubmitCode("$$; $^"));

            KernelEvents.Should().SatisfyRespectively(
                e => e.Should()
                      .BeOfType<CodeSubmissionReceived>()
                      .Which.Code
                      .Should().Be("echo /this/is/a/path"),
                e => e.Should()
                      .BeOfType<CompleteCodeSubmissionReceived>()
                      .Which.Code
                      .Should().Be("echo /this/is/a/path"),
                e => e.Should()
                      .BeOfType<StandardOutputValueProduced>()
                      .Which
                      .FormattedValues
                      .Should()
                      .ContainSingle(f => f.Value == "/this/is/a/path" + Environment.NewLine),
                e => e.Should().BeOfType<CommandSucceeded>(),
                e => e.Should()
                      .BeOfType<CodeSubmissionReceived>()
                      .Which.Code
                      .Should().Be("$$; $^"),
                e => e.Should()
                      .BeOfType<CompleteCodeSubmissionReceived>()
                      .Which.Code
                      .Should().Be("$$; $^"),
                e => e.Should()
                      .BeOfType<StandardOutputValueProduced>()
                      .Which
                      .FormattedValues
                      .Should()
                      .ContainSingle(f => f.Value == "/this/is/a/path" + Environment.NewLine),
                e => e.Should()
                      .BeOfType<StandardOutputValueProduced>()
                      .Which
                      .FormattedValues
                      .Should()
                      .ContainSingle(f => f.Value == "echo" + Environment.NewLine),
                e => e.Should().BeOfType<CommandSucceeded>());
        }

        [Fact]
        public async Task PowerShell_get_history_should_work()
        {
            var kernel = CreateKernel(Language.PowerShell);

            await kernel.SendAsync(new SubmitCode("Get-Verb > $null"));
            await kernel.SendAsync(new SubmitCode("echo bar > $null"));
            var result = await kernel.SendAsync(new SubmitCode("Get-History | % CommandLine"));

            var outputs = result.KernelEvents
                                .ToSubscribedList()
                                .OfType<StandardOutputValueProduced>();

            outputs.Should().SatisfyRespectively(
                e => e.FormattedValues
                      .Should()
                      .ContainSingle(f => f.Value == "Get-Verb > $null" + Environment.NewLine),
                e => e.FormattedValues
                      .Should()
                      .ContainSingle(f => f.Value == "echo bar > $null" + Environment.NewLine));
        }

        [Fact]
        public async Task PowerShell_native_executable_output_is_collected()
        {
            var kernel = CreateKernel(Language.PowerShell);

            var command = Platform.IsWindows
                ? new SubmitCode("ping.exe -n 1 localhost")
                : new SubmitCode("ping -c 1 localhost");

            await kernel.SendAsync(command);

            var outputs = KernelEvents.OfType<StandardOutputValueProduced>();

            outputs.Should().HaveCountGreaterThan(1);
            
            outputs
                .SelectMany(e => e.FormattedValues.Select(v => v.Value))
                .First(s => s.Trim().Length > 0)
                .ToLowerInvariant()
                .Should()
                .Match("*ping*data*");
        }

        [Fact]
        public async Task GetCorrectProfilePaths()
        {
            using var kernel = new PowerShellKernel().UseProfiles();

            // Set variables we will retrieve later.
            await kernel.SubmitCodeAsync("$currentUserCurrentHost = $PROFILE.CurrentUserCurrentHost");
            await kernel.SubmitCodeAsync("$allUsersCurrentHost = $PROFILE.AllUsersCurrentHost");

            kernel.TryGetVariable("currentUserCurrentHost", out object profileObj).Should().BeTrue();
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

                kernel.TryGetVariable(randomVariableName, out object profileObj).Should().BeTrue();

                profileObj.Should().BeOfType<bool>();
                profileObj.As<bool>().Should().BeTrue();
            }
            finally
            {

                File.Delete(_allUsersCurrentHostProfilePath);
            }
        }

        [Fact]
        public async Task Powershell_customobject_is_parsed_for_outdisplay()
        {
            var props = (new Dictionary<string, object>
            {
                { "prop1", "value1" },
                { "prop2", "value2" },
                { "prop3", "value3" }
            });

            var kernel = CreateKernel(Language.PowerShell);
            var result = await kernel.SendAsync(new SubmitCode("[pscustomobject]@{ prop1 = 'value1'; prop2 = 'value2'; prop3 = 'value3' } | Out-Display"));
            var outputs = result.KernelEvents.ToSubscribedList();

            outputs.Should().SatisfyRespectively(
                e => e.Should().BeOfType<CodeSubmissionReceived>(),
                e => e.Should().BeOfType<CompleteCodeSubmissionReceived>(),
                e => e.Should().BeOfType<DisplayedValueProduced>().Which.Value.Should().BeEquivalentTo(props),
                e => e.Should().BeOfType<CommandSucceeded>()
            );
        }
    }
}
