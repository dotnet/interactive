// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting.Tests;
using Microsoft.DotNet.Interactive.Tests;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public partial class MagicCommandTests
    {
        public class markdown
        {
            [Fact]
            public async Task markdown_renders_markdown_content_as_html()
            {
                using var kernel = new CompositeKernel()
                    .UseDefaultMagicCommands();

                var expectedHtml = @"<h1 id=""topic"">Topic!</h1><p>Content</p>";

                using var events = kernel.KernelEvents.ToSubscribedList();

                await kernel.SendAsync(new SubmitCode(
                                           $"%%markdown\n\n# Topic!\nContent"));

                var formatted =
                    events
                        .OfType<DisplayedValueProduced>()
                        .SelectMany(v => v.FormattedValues)
                        .ToArray();

                formatted
                    .Should()
                    .ContainSingle(v =>
                                       v.MimeType == "text/html" &&
                                       v.Value.ToString().Crunch().Equals(expectedHtml));
            }
        }
    }
}