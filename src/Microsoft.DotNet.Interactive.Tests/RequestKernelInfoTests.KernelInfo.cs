// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.PowerShell;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Pocket.For.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Tests;

public class RequestKernelInfoTests
{
    [LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
    public class ForCompositeKernel
    {
        private readonly CompositeDisposable _disposables = new();

        public ForCompositeKernel(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        [Fact]
        public async Task It_returns_kernel_info_for_all_children()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel(),
                new FSharpKernel()
            };

            var result = await kernel.SendAsync(new RequestKernelInfo());

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().ContainSingle<KernelInfoProduced>(e => e.KernelInfo.LocalName == "csharp");
            events.Should().ContainSingle<KernelInfoProduced>(e => e.KernelInfo.LocalName == "fsharp");
        }

        [Fact]
        public async Task It_returns_the_list_of_proxied_kernel_commands_for_a_specified_subkernel()
        {
            using var localCompositeKernel = new CompositeKernel("LOCAL")
            {
                new FakeKernel("fsharp")
            };
            var proxiedCsharpKernel = new CSharpKernel();
            using var remoteCompositeKernel = new CompositeKernel("REMOTE")
            {
                proxiedCsharpKernel,
                new FakeKernel("fsharp")
            };

            ConnectHost.ConnectInProcessHost(
                localCompositeKernel,
                new Uri("kernel://local"),
                remoteCompositeKernel,
                new Uri("kernel://remote"));

            await localCompositeKernel
                  .Host
                  .ConnectProxyKernelOnDefaultConnectorAsync(
                      "remote-fsharp",
                      new Uri("kernel://remote/fsharp"));

            var result = await localCompositeKernel.SendAsync(new RequestKernelInfo("remote-fsharp"));

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().ContainSingle<KernelInfoProduced>();

            throw new NotImplementedException();
        }

        [Fact]
        public void It_returns_the_list_of_subkernels_of_remote_composite()
        {
            // TODO (It_returns_the_list_of_subkernels_of_remote_composite) write test
            throw new NotImplementedException();
        }

        [Fact]
        public async Task When_a_remote_subkernel_is_connected_then_kernel_info_is_updated()
        {
            using var localCompositeKernel = new CompositeKernel
            {
                new FakeKernel("fake")
            };
            using var remoteCompositeKernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            ConnectHost.ConnectInProcessHost(
                localCompositeKernel,
                new Uri("kernel://local"),
                remoteCompositeKernel,
                new Uri("kernel://remote"));
            
            await localCompositeKernel
                  .Host
                  .ConnectProxyKernelOnDefaultConnectorAsync(
                      "remote-fsharp",
                       new Uri("kernel://remote/fsharp"));

            var result = await localCompositeKernel.SendAsync(
                             new SubmitCode(@"Kernel.Root.Add(new Microsoft.DotNet.Interactive.FSharp.FSharpKernel());",
                                            targetKernelName: "csharp"));

            // TODO (When_a_remote_kernel_is_added_via_an_extension_then_kernel_info_is_updated) write test
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Unproxied_kernels_have_a_URI()
        {
            using var localCompositeKernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            localCompositeKernel.ConnectInProcessHost(new Uri("kernel://somewhere/"));

            var result = await localCompositeKernel.SendAsync(new RequestKernelInfo());

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<KernelInfoProduced>()
                  .Which
                  .KernelInfo
                  .Uri
                  .Should()
                  .Be(new Uri("kernel://somewhere/csharp"));
        }

        [Fact]
        public void A_proxy_to_a_local_kernel_cannot_be_created()
        {
            

            // TODO (A_proxy_to_a_local_kernel_cannot_be_created) write test
            throw new NotImplementedException();
        }
    }

    public class ForUnparentedKernel
    {
        [Fact]
        public async Task It_returns_the_list_of_intrinsic_kernel_commands()
        {
            using var kernel = new CSharpKernel();

            var result = await kernel.SendAsync(new RequestKernelInfo());

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<KernelInfoProduced>()
                  .Which
                  .KernelInfo
                  .SupportedKernelCommands
                  .Select(info => info.Name)
                  .Should()
                  .Contain(
                      nameof(SubmitCode));
        }

        [Fact]
        public async Task It_returns_language_info_for_csharp_kernel()
        {
            using var kernel = new CSharpKernel();

            var result = await kernel.SendAsync(new RequestKernelInfo());

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<KernelInfoProduced>()
                  .Which
                  .KernelInfo
                  .LanguageName
                  .Should()
                  .Be("C#");
        }

        [Fact]
        public async Task It_returns_language_info_for_fsharp_kernel()
        {
            using var kernel = new FSharpKernel();

            var result = await kernel.SendAsync(new RequestKernelInfo());

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<KernelInfoProduced>()
                  .Which
                  .KernelInfo
                  .LanguageName
                  .Should()
                  .Be("F#");
        }

        [Fact]
        public async Task It_returns_language_info_for_PowerShell_kernel()
        {
            using var kernel = new PowerShellKernel();

            var result = await kernel.SendAsync(new RequestKernelInfo());

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<KernelInfoProduced>()
                  .Which
                  .KernelInfo
                  .LanguageName
                  .Should()
                  .Be("PowerShell");
        }

        [Fact]
        public async Task It_returns_the_list_of_directive_commands()
        {
            using var kernel = new CSharpKernel()
                               .UseNugetDirective()
                               .UseWho();

            var result = await kernel.SendAsync(new RequestKernelInfo());

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<KernelInfoProduced>()
                  .Which
                  .KernelInfo
                  .SupportedDirectives
                  .Select(info => info.Name)
                  .Should()
                  .Contain("#!who", "#!who", "#r");
        }

        [Fact]
        public async Task It_returns_the_list_of_dynamic_kernel_commands()
        {
            using var kernel = new FakeKernel();
            kernel.RegisterCommandHandler<RequestHoverText>((_, _) => Task.CompletedTask);
            kernel.RegisterCommandHandler<RequestDiagnostics>((_, _) => Task.CompletedTask);
            kernel.RegisterCommandHandler<CustomCommandTypes.FirstSubmission.MyCommand>((_, _) => Task.CompletedTask);

            var result = await kernel.SendAsync(new RequestKernelInfo());

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<KernelInfoProduced>()
                  .Which
                  .KernelInfo
                  .SupportedKernelCommands
                  .Select(c => c.Name)
                  .Should()
                  .Contain(
                      nameof(RequestHoverText),
                      nameof(RequestDiagnostics),
                      nameof(CustomCommandTypes.FirstSubmission.MyCommand));
        }
    }
}