﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;
using FluentAssertions;
using System.Linq;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
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

            kernel.TryGetValue("x", out object fi).Should().BeTrue();

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
                                       e => e.Should().BeOfType<DiagnosticsProduced>()
                                              .Which.Diagnostics.Count.Should().Be(0),
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
                e => e.Should().BeOfType<DiagnosticsProduced>()
                      .Which.Diagnostics.Count.Should().Be(0),
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
                e => e.Should().BeOfType<DiagnosticsProduced>()
                      .Which.Diagnostics.Count.Should().Be(0),
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

            var command = new SubmitCode("dotnet --help");

            await kernel.SendAsync(command);

            var outputs = KernelEvents.OfType<StandardOutputValueProduced>();

            outputs.Should().HaveCountGreaterThan(1);
            
            string.Join("", 
                outputs
                .SelectMany(e => e.FormattedValues.Select(v => v.Value))
                ).ToLowerInvariant()
                .Should()
                .ContainAll("build-server", "restore");
        }

        [Fact]
        public async Task GetCorrectProfilePaths()
        {
            using var kernel = new PowerShellKernel().UseProfiles();

            // Set variables we will retrieve later.
            await kernel.SubmitCodeAsync("$currentUserCurrentHost = $PROFILE.CurrentUserCurrentHost");
            await kernel.SubmitCodeAsync("$allUsersCurrentHost = $PROFILE.AllUsersCurrentHost");

            kernel.TryGetValue("currentUserCurrentHost", out object profileObj).Should().BeTrue();
            profileObj.Should().BeOfType<string>();
            string currentUserCurrentHost = profileObj.As<string>();

            // Get $PROFILE default.
            kernel.TryGetValue("PROFILE", out profileObj).Should().BeTrue();
            profileObj.Should().BeOfType<string>();
            string profileDefault = profileObj.As<string>();

            // Check that $PROFILE is not null or empty and it is the same as
            // $PROFILE.CurrentUserCurrentHost
            profileDefault.Should().NotBeNullOrEmpty();
            profileDefault.Should().Be(currentUserCurrentHost);

            kernel.TryGetValue("allUsersCurrentHost", out profileObj).Should().BeTrue();
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

                kernel.TryGetValue(randomVariableName, out object profileObj).Should().BeTrue();

                profileObj.Should().BeOfType<bool>();
                profileObj.As<bool>().Should().BeTrue();
            }
            finally
            {

                File.Delete(_allUsersCurrentHostProfilePath);
            }
        }

        [Fact]
        public async Task Powershell_customobject_is_formatted_for_outdisplay()
        {
            var kernel = CreateKernel(Language.PowerShell);
            var result = await kernel.SendAsync(new SubmitCode("[pscustomobject]@{ prop1 = 'value1'; prop2 = 'value2'; prop3 = 'value3' } | Out-Display"));
            var outputs = result.KernelEvents.ToSubscribedList();

            string mimeType = "text/html";
            string formattedHtml = $"<table><thead><tr><th><i>key</i></th><th>value</th></tr></thead><tbody><tr><td>prop1</td><td>value1</td></tr><tr><td>prop2</td><td>value2</td></tr><tr><td>prop3</td><td>value3</td></tr></tbody></table>";
            FormattedValue fv = new FormattedValue(mimeType, formattedHtml);

            outputs.Should().SatisfyRespectively(
                e => e.Should().BeOfType<CodeSubmissionReceived>(),
                e => e.Should().BeOfType<CompleteCodeSubmissionReceived>(),
                e => e.Should().BeOfType<DiagnosticsProduced>().Which.Diagnostics.Count.Should().Be(0),
                e => e.Should().BeOfType<DisplayedValueProduced>().Which.FormattedValues.ElementAt(0).Should().BeEquivalentToRespectingRuntimeTypes(fv),
                e => e.Should().BeOfType<CommandSucceeded>()
            );
        }

        [Fact]
        public async Task RequestValueInfos_only_returns_user_defined_values()
        {
            using var kernel = CreateKernel(Language.PowerShell);
            await kernel.SendAsync(new SubmitCode("$theAnswer = 42"));

            var result = await kernel.SendAsync(new RequestValueInfos(kernel.Name));
            var events = result.KernelEvents.ToSubscribedList();
            events
                .Should()
                .ContainSingle<ValueInfosProduced>()
                .Which
                .ValueInfos
                .Should()
                .ContainSingle()
                .Which
                .Name
                .Should()
                .Be("theAnswer");
        }
    }
}
