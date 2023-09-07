// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Tests;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

#pragma warning disable 8509 // don't warn on incomplete pattern matches
namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

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
                Language.CSharp => new CSharpKernel().UseWho() as Kernel,
                Language.FSharp => new FSharpKernel().UseWho()
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
                    "var y = \"hi>!\";",
                    "var z = new object[] { x, y };",
                },
                Language.FSharp => new[]
                {
                    "let mutable x = 1",
                    "x <- 2",
                    "let y = \"hi>!\"",
                    "let z = [| x :> obj; y :> obj |]"
                },
            };

            foreach (var command in commands)
            {
                await kernel.SendAsync(new SubmitCode(command));
            }

            await kernel.SendAsync(new SubmitCode("#!whos"));

            var html = events.Should()
                                           .ContainSingle(e => e is DisplayedValueProduced)
                                           .Which
                                           .As<DisplayedValueProduced>()
                                           .FormattedValues
                                           .Should()
                                           .ContainSingle(v => v.MimeType == "text/html")
                                           .Which
                                           .Value
                                           .As<string>()
                                           .RemoveStyleElement();

            html.Should()
                .ContainEquivalentHtmlFragments(
                    """
                        <td>x</td>
                        <td><span><a href="https://docs.microsoft.com/dotnet/api/system.int32?view=net-7.0">System.Int32</a></span>
                        </td>
                        <td>
                            <div class="dni-plaintext">
                                <pre>2</pre>
                            </div>
                        </td>
                        """,
                    """
                        <td>y</td>
                        <td><span><a
                                    href="https://docs.microsoft.com/dotnet/api/system.string?view=net-7.0">System.String</a></span>
                        </td>
                        <td>
                            <div class="dni-plaintext">
                                <pre>hi&gt;!</pre>
                            </div>
                        </td>
                        """,
                    """
                        <td>z</td>
                            <td><span><a
                                        href="https://docs.microsoft.com/dotnet/api/system.object[]?view=net-7.0">System.Object[]</a></span>
                            </td>
                            <td>
                                <div class="dni-plaintext">
                                    <pre>Object[]
                        - 2
                        - hi&gt;!</pre>
                                    </div>
                        </td>
                    """
                    );
        }

        [Theory]
        [InlineData(Language.CSharp)]
        [InlineData(Language.FSharp)]
        public async Task who_lists_the_names_of_variables_in_scope(Language language)
        {
            using var baseKernel = language switch
            {
                Language.CSharp => new CSharpKernel().UseWho() as Kernel,
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

            await kernel.SendAsync(new SubmitCode("#!who"));

            events.Should()
                .ContainSingle<DisplayedValueProduced>()
                .Which
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