// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Tests;

[TestClass]
public class JavaScriptKernelTests
{
    [TestMethod]
    public async Task javascript_kernel_emits_code_as_it_was_given()
    {
        using var kernel = new CompositeKernel
        {
            new JavaScriptKernel()
        };

        var scriptContent = "alert('Hello World!');";

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SendAsync(new SubmitCode($"#!javascript\n{scriptContent}"));

        var formatted =
            events
                .OfType<DisplayedValueProduced>()
                .Select(v => v.Value)
                .Cast<ScriptContent>()
                .ToArray();

        formatted
            .Should()
            .ContainSingle()
            .Which
            .ScriptValue
            .Should()
            .Be($"{scriptContent}");
    }


    [TestMethod]
    public async Task javascript_kernel_forwards_commands_to_frontend()
    {
        var client = new TestClient();
        using var kernel = new CompositeKernel
        {
            new JavaScriptKernel(client)
        };
            
        kernel.FindKernelByName(JavaScriptKernel.DefaultKernelName).RegisterCommandType<CustomCommand>();

        using var events = kernel.KernelEvents.ToSubscribedList();

        var command = new CustomCommand(JavaScriptKernel.DefaultKernelName);
            
        await kernel.SendAsync(command, CancellationToken.None);

        client.ForwardedCommands.Should().Contain(command);
    }

    public class CustomCommand : KernelCommand
    {
        public CustomCommand(string targetKernelName) : base(targetKernelName: targetKernelName)
        {
                
        }
    }

    public class TestClient : KernelClientBase
    {
        public List<KernelCommand> ForwardedCommands { get; } = new();
        public override IObservable<KernelEvent> KernelEvents { get; } = new Subject<KernelEvent>();
        public override Task SendAsync(KernelCommand command, string token = null)
        {
            ForwardedCommands.Add(command);
            return Task.CompletedTask;
        }
    }
}