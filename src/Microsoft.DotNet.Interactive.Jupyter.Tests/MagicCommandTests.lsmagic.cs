// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public partial class MagicCommandTests
    {
        public class lsmagic
        {
            [Fact]
            public async Task lsmagic_lists_registered_magic_commands()
            {
                using var kernel = new CompositeKernel()
                                   .UseDefaultMagicCommands()
                                   .LogEventsToPocketLogger();

                kernel.AddDirective(new Command("#!one"));
                kernel.AddDirective(new Command("#!two"));
                kernel.AddDirective(new Command("#!three"));

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
                subkernel1.AddDirective(new Command("#!from-subkernel-1"));
                var subkernel2 = new FSharpKernel();
                subkernel2.AddDirective(new Command("#!from-subkernel-2"));

                using var compositeKernel = new CompositeKernel
                                            {
                                                subkernel1,
                                                subkernel2
                                            }
                                            .UseDefaultMagicCommands()
                                            .LogEventsToPocketLogger();
                compositeKernel.DefaultKernelName = "csharp";

                compositeKernel.AddDirective(new Command("#!from-compositekernel"));

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

            kernel.AddDirective(new Command("#!hidden")
            {
                IsHidden = true
            });

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

        [Fact]
        public async Task r_executes_even_with_comment()
        {
            // Make sure the considered package gets successfully installed without comments afterwards
            using var kernel1 = new FSharpKernel().UseNugetDirective();
            var code1 = new SubmitCode("#r \"nuget:Plotly.NET.Interactive, 2.0.0-beta8\"");
            var res1 = await kernel1.SendAsync(code1);
            Expression<Func<KernelEvent, bool>> f1 = ev => ev is DisplayEvent && ((DisplayEvent)ev).ToDisplayString("text/plain").Contains("Installing");
            res1.KernelEvents.ToSubscribedList().Should().Contain(f1);

            // Now, let us add a comment after the command
            using var kernel2 = new FSharpKernel().UseNugetDirective();
            var code2 = new SubmitCode("#r \"nuget:Plotly.NET.Interactive, 2.0.0-beta8\" // ducks best!");
            var res2 = await kernel2.SendAsync(code2);
            Expression<Func<KernelEvent, bool>> f2 = ev => ev is DisplayEvent && ((DisplayEvent)ev).ToDisplayString("text/plain").Contains("Installing");
            res2.KernelEvents.ToSubscribedList().Should().Contain(f2);
        }
    }
}