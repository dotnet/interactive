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

public class KernelInfoTests
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
                remoteCompositeKernel);

            var remoteKernelUri = new Uri("kernel://remote/fsharp");

            await localCompositeKernel
                  .Host
                  .ConnectProxyKernelOnDefaultConnectorAsync(
                      "proxied-fsharp",
                      remoteKernelUri);

            var result = await localCompositeKernel.SendAsync(
                             new RequestKernelInfo(remoteKernelUri));

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<KernelInfoProduced>()
                  .Which
                  .KernelInfo
                  .Should()
                  .BeEquivalentTo(new
                  {
                      LanguageName = "fsharp",
                      Uri = remoteKernelUri
                  }, c => c.ExcludingMissingMembers());
        }

        [Fact]
        public async Task It_returns_info_about_unproxied_subkernels_of_remote_composite()
        {
            using var localCompositeKernel = new CompositeKernel("LOCAL-COMPOSITE")
            {
                new FakeKernel("local-fake")
            };
            using var remoteCompositeKernel = new CompositeKernel("REMOTE-COMPOSITE")
            {
                new FakeKernel("remote-fake")
            };

            ConnectHost.ConnectInProcessHost(
                localCompositeKernel,
                remoteCompositeKernel);

            await localCompositeKernel
                  .Host
                  .ConnectProxyKernelOnDefaultConnectorAsync(
                      "remote-composite",
                      remoteCompositeKernel.Host.Uri);

            var result = await localCompositeKernel.SendAsync(new RequestKernelInfo());

            var events = result.KernelEvents
                               .ToSubscribedList()
                               .OfType<KernelInfoProduced>();

            events
                .Select(k => k.KernelInfo.Uri)
                .Should()
                .Contain(new Uri("kernel://remote/remote-fake"));
        }
        
        [Fact]
        public async Task Unproxied_kernels_have_a_URI()
        {
            using var localCompositeKernel = new CompositeKernel
            {
                new CSharpKernel()
            };

            localCompositeKernel.ConnectInProcessHost(new Uri("kernel://local/"));

            var result = await localCompositeKernel.SendAsync(new RequestKernelInfo());

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                  .ContainSingle<KernelInfoProduced>()
                  .Which
                  .KernelInfo
                  .Uri
                  .Should()
                  .Be(new Uri("kernel://local/csharp"));
        }

        [Fact]
        public async Task ProxyKernels_have_a_local_uri()
        {
            using var localCompositeKernel = new CompositeKernel
            {
                new FakeKernel("csharp")
            };
            using var remoteCompositeKernel = new CompositeKernel
            {
                new FakeKernel("python")
            };

            ConnectHost.ConnectInProcessHost(
                localCompositeKernel,
                remoteCompositeKernel);

            var proxyKernel = await localCompositeKernel
                                    .Host
                                    .ConnectProxyKernelOnDefaultConnectorAsync(
                                        "python",
                                        new Uri("kernel://remote/python"));

            proxyKernel.KernelInfo
                       .Uri
                       .Should()
                       .Be(new Uri("kernel://local/python"));
        }

        [Fact]
        public async Task ProxyKernels_have_a_remote_uri()
        {
            using var localCompositeKernel = new CompositeKernel
            {
                new FakeKernel("csharp")
            };
            using var remoteCompositeKernel = new CompositeKernel
            {
                new FakeKernel("python")
            };

            ConnectHost.ConnectInProcessHost(
                localCompositeKernel,
                remoteCompositeKernel);

            var proxyKernel = await localCompositeKernel
                                    .Host
                                    .ConnectProxyKernelOnDefaultConnectorAsync(
                                        "python",
                                        new Uri("kernel://remote/python"));

            proxyKernel.KernelInfo
                       .RemoteUri
                       .Should()
                       .Be(new Uri("kernel://remote/python"));
        }

        [Fact]
        public async Task When_kernel_info_is_requested_from_proxy_then_ProxyKernel_kernel_info_is_updated()
        {
            using var localCompositeKernel = new CompositeKernel();
            
            var remoteCsharpKernel = new CSharpKernel();
            
            using var remoteCompositeKernel = new CompositeKernel
            {
                remoteCsharpKernel
            };

            ConnectHost.ConnectInProcessHost(
                localCompositeKernel,
                remoteCompositeKernel);

            var remoteKernelUri = new Uri("kernel://remote/csharp");

            var proxyKernel = await localCompositeKernel
                                    .Host
                                    .ConnectProxyKernelOnDefaultConnectorAsync(
                                        "csharp",
                                        remoteKernelUri);

            await localCompositeKernel.SendAsync(
                new RequestKernelInfo(remoteKernelUri));

            proxyKernel
                .KernelInfo
                .LocalName
                .Should()
                .Be("csharp");

            proxyKernel
                  .KernelInfo
                  .SupportedKernelCommands
                  .Select(c => c.Name)
                  .Should()
                  .Contain(remoteCsharpKernel.KernelInfo.SupportedKernelCommands.Select(c => c.Name));
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