// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.LanguageService;
using Xunit;

namespace Microsoft.DotNet.Interactive.App.Tests
{
    public class LanguageServiceTests
    {
        [Fact]
        public async Task textDocument_hover_handles_growing_script_offsets()
        {
            using var kernel = new CSharpKernel();

            // evaluate some code
            await kernel.SubmitCodeAsync("var one = 1;");

            // get hover info
            var code = "Console.WriteLine(one);";
            //                             ^ (0, 20)
            var hoverRequest = new HoverParams()
            {
                TextDocument = TextDocument.FromDocumentContents(code),
                Position = new Position(0, 20),
            };

            var hoverResponse = await kernel.Hover(hoverRequest);

            // ensure the position information is correct, e.g., line 0 not line 1
            using var _ = new AssertionScope();
            hoverResponse.Range.Start.Line.Should().Be(0);
            hoverResponse.Range.Start.Character.Should().Be(18);
            hoverResponse.Range.End.Line.Should().Be(0);
            hoverResponse.Range.End.Character.Should().Be(21);
        }
    }
}
