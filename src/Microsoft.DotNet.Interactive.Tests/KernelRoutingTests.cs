﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Connection;
using Microsoft.DotNet.Interactive.Server;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class KernelRoutingTests : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        public KernelRoutingTests(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [FactSkipLinux]
        public async Task non_default_remote_subkernel_is_directly_addressable()
        {
            var hitLocalPwsh = false;
            var hitLocalFSharp = false;
            using var localCompositeKernel = new CompositeKernel
            {
                new FakeKernel("pwsh")
                {
                    Handle = (command, context) =>
                    {
                        hitLocalPwsh = true;
                        return Task.CompletedTask;
                    }
                },
                new FakeKernel("fsharp")
                {
                    Handle = (command, context) =>
                    {
                        hitLocalFSharp = true;
                        return Task.CompletedTask;
                    }
                },
            }.UseKernelClientConnection(new ConnectNamedPipe());

            localCompositeKernel.DefaultKernelName = "pwsh";

            var pipeName = Guid.NewGuid().ToString();
            
            var hitRemoteCSharp = false;
            var hitRemoteFSharp = false;
            using var remoteCompositeKernel = new CompositeKernel
            {
                new FakeKernel("csharp")
                {
                    Handle = (command, context) =>
                    {
                        hitRemoteCSharp = true;
                        return Task.CompletedTask;
                    }
                },
                new FakeKernel("fsharp")
                {
                    Handle = (command, context) =>
                    {
                        hitRemoteFSharp = true;
                        return Task.CompletedTask;
                    }
                },
            };

            remoteCompositeKernel.DefaultKernelName = "fsharp";

            StartServer(remoteCompositeKernel, pipeName);

            using var events = localCompositeKernel.KernelEvents.ToSubscribedList();

            var connectToRemoteKernel = new SubmitCode($"#!connect named-pipe --kernel-name newKernelName --pipe-name {pipeName}");
            var codeSubmissionForRemoteKernel = new SubmitCode(@"
#!newKernelName/csharp
var x = 1 + 1;
x");

            await localCompositeKernel.SendAsync(connectToRemoteKernel);
            await localCompositeKernel.SendAsync(codeSubmissionForRemoteKernel);

            using var _ = new AssertionScope();
            events.Should().NotContainErrors();

            hitLocalPwsh.Should().BeFalse();
            hitLocalFSharp.Should().BeFalse();

            hitRemoteCSharp.Should().BeTrue();
            hitRemoteFSharp.Should().BeFalse();
        }
        

        [FactSkipLinux]
        public async Task proxyKernel_does_not_perform_split_if_all_parts_go_to_same_targetKernel_as_the_original_command()
        {
            var handledCommands = new List<KernelCommand>();
            using var remoteCompositeKernel = new CompositeKernel
            {
               
                new FakeKernel("csharp")
                {
                    Handle = (command, _) =>
                    {
                        handledCommands.Add(command);
                        return Task.CompletedTask;
                    }
                },
                new FakeKernel("fsharp")
                {
                    Handle = (_, _) => Task.CompletedTask
                },
            };

            remoteCompositeKernel.DefaultKernelName = "csharp";
            var pipeName = Guid.NewGuid().ToString();

            StartServer(remoteCompositeKernel, pipeName);
            var connection = new ConnectNamedPipe();

            var proxyKernel = await connection.ConnectKernelAsync(new NamedPipeConnection
            {
                KernelName = "proxyKernel",
                PipeName = pipeName
            }, null);
            
            var code = @"#i ""nuget:source1""
#i ""nuget:source2""
#r ""nuget:package1""
#r ""nuget:package2""

Console.WriteLine(1);";

            var command = new SubmitCode(code, proxyKernel.Name);
            await proxyKernel.SendAsync(command);

            handledCommands.Should().ContainSingle<SubmitCode>();
        }


        [FactSkipLinux]
        public async Task proxyKernel_does_not_perform_split_if_all_parts_go_to_same_targetKernel_original_command_has_not_target_kernel()
        {

            var handledCommands = new List<KernelCommand>();
            using var remoteCompositeKernel = new CompositeKernel
            {

                new FakeKernel("csharp")
                {
                    Handle = (command, _) =>
                    {
                        handledCommands.Add(command);
                        return Task.CompletedTask;
                    }
                },
                new FakeKernel("fsharp")
                {
                    Handle = (_, _) => Task.CompletedTask
                },
            };

            remoteCompositeKernel.DefaultKernelName = "csharp";
            var pipeName = Guid.NewGuid().ToString();

            StartServer(remoteCompositeKernel, pipeName);
            var connection = new ConnectNamedPipe();

            var proxyKernel = await connection.ConnectKernelAsync(new NamedPipeConnection
            {
                KernelName = "proxyKernel",
                PipeName = pipeName
            }, null);

            var code = @"#i ""nuget:source1""
#i ""nuget:source2""
#r ""nuget:package1""
#r ""nuget:package2""

Console.WriteLine(1);";

            var command = new SubmitCode(code);
            await proxyKernel.SendAsync(command);

            handledCommands.Should().ContainSingle<SubmitCode>();
        }

        void StartServer(Kernel remoteKernel, string pipeName) => remoteKernel.UseNamedPipeKernelServer(pipeName, new DirectoryInfo(Environment.CurrentDirectory));
    }
}
