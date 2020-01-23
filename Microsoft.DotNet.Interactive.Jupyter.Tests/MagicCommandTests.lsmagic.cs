// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests;
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

                kernel.AddDirective(new Command("%%one"));
                kernel.AddDirective(new Command("%%two"));
                kernel.AddDirective(new Command("%%three"));

                using var events = kernel.KernelEvents.ToSubscribedList();

                await kernel.SendAsync(new SubmitCode("%lsmagic"));

                events.Should()
                      .ContainSingle(e => e is DisplayedValueProduced)
                      .Which
                      .As<DisplayedValueProduced>()
                      .Value
                      .ToDisplayString("text/html")
                      .Should()
                      .ContainAll("%lsmagic", "%%one", "%%three", "%%two");
            }

            [Fact]
            public async Task lsmagic_lists_registered_magic_commands_in_subkernels()
            {
                var subkernel1 = new CSharpKernel();
                subkernel1.AddDirective(new Command("%%from-subkernel-1"));
                var subkernel2 = new FSharpKernel();
                subkernel2.AddDirective(new Command("%%from-subkernel-2"));

                using var compositeKernel = new CompositeKernel
                                            {
                                                subkernel1,
                                                subkernel2
                                            }
                                            .UseDefaultMagicCommands()
                                            .LogEventsToPocketLogger();

                compositeKernel.AddDirective(new Command("%%from-compositekernel"));

                using var events = compositeKernel.KernelEvents.ToSubscribedList();

                await compositeKernel.SendAsync(new SubmitCode("%lsmagic"));

                var valueProduceds = events.OfType<DisplayedValueProduced>().ToArray();

                valueProduceds[0].Value
                                 .ToDisplayString("text/html")
                                 .Should()
                                 .ContainAll("%lsmagic",
                                             "%%csharp",
                                             "%%fsharp",
                                             "%%from-compositekernel");

                valueProduceds[1].Value
                                 .ToDisplayString("text/html")
                                 .Should()
                                 .ContainAll("%lsmagic",
                                             "%%from-subkernel-1");
                valueProduceds[2].Value
                                 .ToDisplayString("text/html")
                                 .Should()
                                 .ContainAll("%lsmagic",
                                             "%%from-subkernel-2");
            }
        }
    }
}