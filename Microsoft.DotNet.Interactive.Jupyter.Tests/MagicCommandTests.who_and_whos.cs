// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests;
using Xunit;

#pragma warning disable 8509 // don't warn on incomplete pattern matches
namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public partial class MagicCommandTests
    {
        public class who_and_whos
        {
            [Theory]
            [InlineData(Language.CSharp)]
            [InlineData(Language.FSharp)]
            public async Task whos_lists_the_names_and_values_of_variables_in_scope(Language language)
            {
                using var baseKernel = language switch
                {
                    Language.CSharp => new CSharpKernel().UseWho() as KernelBase,
                    Language.FSharp => new FSharpKernel().UseWho(),
                };
                using var kernel = new CompositeKernel
                    {
                        baseKernel
                    }
                    .LogEventsToPocketLogger();

                using var events = kernel.KernelEvents.ToSubscribedList();

                var commands = language switch
                {
                    Language.CSharp => new[]
                    {
                        "var x = 1;",
                        "x = 2;",
                        "var y = \"hi!\";",
                        "var z = new object[] { x, y };",
                    },
                    Language.FSharp => new[]
                    {
                        "let mutable x = 1",
                        "x <- 2",
                        "let y = \"hi!\"",
                        "let z = [| x :> obj; y :> obj |]",
                    },
                };

                foreach (var command in commands)
                {
                    await kernel.SendAsync(new SubmitCode(command));
                }

                await kernel.SendAsync(new SubmitCode(@"%whos"));

                events.Should()
                      .ContainSingle(e => e is DisplayedValueProduced)
                      .Which
                      .As<DisplayedValueProduced>()
                      .FormattedValues
                      .Should()
                      .ContainSingle(v => v.MimeType == "text/html")
                      .Which
                      .Value
                      .As<string>()
                      .Should()
                      .ContainAll(
                          "<td>x</td><td>System.Int32</td><td>2</td>",
                          "<td>y</td><td>System.String</td><td>hi!</td>",
                          "<td>z</td><td>System.Object[]</td><td>[ 2, hi! ]</td>");
            }

            [Theory]
            [InlineData(Language.CSharp)]
            [InlineData(Language.FSharp)]
            public async Task who_lists_the_names_of_variables_in_scope(Language language)
            {
                using var baseKernel = language switch
                {
                    Language.CSharp => new CSharpKernel().UseWho() as KernelBase,
                    Language.FSharp => new FSharpKernel().UseWho(),
                };
                using var kernel = new CompositeKernel
                    {
                        baseKernel
                    }
                    .LogEventsToPocketLogger();

                using var events = kernel.KernelEvents.ToSubscribedList();

                var commands = language switch
                {
                    Language.CSharp => new[]
                    {
                        "var x = 1;",
                        "x = 2;",
                        "var y = \"hi!\";",
                        "var z = new object[] { x, y };",
                    },
                    Language.FSharp => new[]
                    {
                        "let mutable x = 1",
                        "x <- 2",
                        "let y = \"hi!\"",
                        "let z = [| x :> obj; y :> obj |]",
                    },
                };

                foreach (var command in commands)
                {
                    await kernel.SendAsync(new SubmitCode(command));
                }

                await kernel.SendAsync(new SubmitCode(@"%who"));

                events.Should()
                      .ContainSingle(e => e is DisplayedValueProduced)
                      .Which
                      .As<DisplayedValueProduced>()
                      .FormattedValues
                      .Should()
                      .ContainSingle(v => v.MimeType == "text/html")
                      .Which
                      .Value
                      .As<string>()
                      .Should()
                      .ContainAll("x", "y", "z");
            }
        }
    }
}