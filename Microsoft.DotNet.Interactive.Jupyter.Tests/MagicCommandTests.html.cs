// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests;
using Pocket;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public partial class MagicCommandTests
    {
        public class html
        {
            [Fact]
            public async Task html_emits_string_as_content_within_a_script_element()
            {
                using var kernel = new CompositeKernel()
                    .UseDefaultMagicCommands();

                var html = "<b>hello!</b>";

                using var events = kernel.KernelEvents.ToSubscribedList();

                await kernel.SendAsync(new SubmitCode(
                                           $"%%html\n\n{html}"));

                var formatted =
                    events
                        .OfType<DisplayedValueProduced>()
                        .SelectMany(v => v.FormattedValues)
                        .ToArray();

                Logger.Log.Info(events.ToDisplayString());

                formatted
                    .Should()
                    .ContainSingle(v =>
                                       v.MimeType == "text/html" &&
                                       v.Value.ToString().Equals(html));
            }
        }
    }
}