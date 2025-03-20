// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Tests;

[TestClass]
public class QuitCommandTests : LanguageKernelTestBase
{
    public QuitCommandTests(TestContext output) : base(output)
    {
    }

    [TestMethod]
    public async Task quit_command_fails_when_not_configured()
    {
        var kernel = CreateKernel();

        var quit = new Quit();

        await kernel.SendAsync(quit);

        using var _ = new AssertionScope();

        KernelEvents
            .Should().ContainSingle<CommandFailed>()
            .Which
            .Command
            .Should()
            .Be(quit);

        KernelEvents
            .Should().ContainSingle<CommandFailed>()
            .Which
            .Exception
            .Should()
            .BeOfType<InvalidOperationException>();
    }

    [TestMethod]
    public async Task Quit_command_bypasses_work_in_progress()
    {
        var quitRan = false;

        var kernel = CreateKernel().UseQuitCommand(() =>
        {
            quitRan = true;
            return Task.CompletedTask;
        });

        var workInProgress = kernel.SendAsync(new CancelCommandTests.CancellableCommand());

        await kernel.SendAsync(new Quit());

        quitRan.Should().BeTrue();
    }
}