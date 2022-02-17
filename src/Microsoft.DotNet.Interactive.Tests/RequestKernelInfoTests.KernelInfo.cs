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
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests;

public class RequestKernelInfoTests
{
    public class ForCompositeKernel
    {
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

            // TODO (It_returns_kernel_info_for_all_children) write test
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
        public async Task It_returns_the_list_of_directives_commands()
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
        public async Task Kernel_info_returns_the_list_of_dynamic_kernel_commands()
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