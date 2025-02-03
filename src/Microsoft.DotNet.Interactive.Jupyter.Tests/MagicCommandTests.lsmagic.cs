// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Pocket;
using Pocket.For.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public partial class MagicCommandTests
{
    [LogToPocketLogger(FileNameEnvironmentVariable = "POCKETLOGGER_LOG_PATH")]
    public class LSMmagic : IDisposable
    {
        private readonly CompositeDisposable _disposables = new();

        public LSMmagic(ITestOutputHelper output)
        {
            _disposables.Add(output.SubscribeToPocketLogger());
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }

        [Fact]
        public async Task lsmagic_lists_registered_magic_commands()
        {
            using var kernel = new CompositeKernel()
                               .UseDefaultMagicCommands()
                               .LogEventsToPocketLogger();

            kernel.AddDirective(new KernelActionDirective("#!one"), (_, _) => Task.CompletedTask);
            kernel.AddDirective(new KernelActionDirective("#!two"), (_, _) => Task.CompletedTask);
            kernel.AddDirective(new KernelActionDirective("#!three"), (_, _) => Task.CompletedTask);

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SendAsync(new SubmitCode("#!lsmagic"));

            events.Should()
                .ContainSingle(e => e is DisplayedValueProduced)
                .Which
                .As<DisplayedValueProduced>()
                .Value
                .ToDisplayString("text/html")
                .Should()
                .ContainAll("#!lsmagic", "#!one", "#!three", "#!two");
        }

        [Fact]
        public async Task lsmagic_lists_registered_magic_commands_in_subkernels()
        {
            var subkernel1 = new CSharpKernel();
            subkernel1.AddDirective(new KernelActionDirective("#!from-subkernel-1"), (_, _) => Task.CompletedTask);
            var subkernel2 = new FSharpKernel();
            subkernel2.AddDirective(new KernelActionDirective("#!from-subkernel-2"), (_, _) => Task.CompletedTask);

            using var compositeKernel = new CompositeKernel
                {
                    subkernel1,
                    subkernel2
                }
                .UseDefaultMagicCommands()
                .LogEventsToPocketLogger();
            compositeKernel.DefaultKernelName = "csharp";

            compositeKernel.AddDirective(new KernelActionDirective("#!from-compositekernel"), (_, _) => Task.CompletedTask);

            using var events = compositeKernel.KernelEvents.ToSubscribedList();

            await compositeKernel.SendAsync(new SubmitCode("#!lsmagic"));

            var valueProduceds = events.OfType<DisplayedValueProduced>().ToArray();

            valueProduceds[0].Value
                             .ToDisplayString("text/html")
                             .Should()
                             .ContainAll("#!lsmagic",
                                         "#!csharp",
                                         "#!fsharp",
                                         "#!from-compositekernel");

            valueProduceds[1].Value
                             .ToDisplayString("text/html")
                             .Should()
                             .ContainAll("#!lsmagic",
                                         "#!from-subkernel-1");

            valueProduceds[2].Value
                             .ToDisplayString("text/html")
                             .Should()
                             .ContainAll("#!lsmagic",
                                         "#!from-subkernel-2");
        }
    }

    [Fact]
    public async Task lsmagic_does_not_list_hidden_commands()
    {
        using var kernel = new CompositeKernel()
                           .UseDefaultMagicCommands()
                           .LogEventsToPocketLogger();

        kernel.AddDirective(new KernelActionDirective("#!hidden")
        {
            Hidden = true
        }, (_, _) => Task.CompletedTask);

        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SendAsync(new SubmitCode("#!lsmagic"));

        events.Should()
              .ContainSingle(e => e is DisplayedValueProduced)
              .Which
              .As<DisplayedValueProduced>()
              .Value
              .ToDisplayString("text/html")
              .Should()
              .NotContain("#!hidden");
    }
}