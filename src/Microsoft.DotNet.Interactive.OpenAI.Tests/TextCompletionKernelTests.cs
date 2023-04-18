// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.OpenAI.Tests;

public class TextCompletionKernelTests
{
    [Fact]
    public async Task Can_use_prompts()
    {
        var semanticKernel = KernelBuilder.BuildSemanticKernel();

        var promptKernel = new PromptKernel(semanticKernel, "prompt");
        var textKernel = new TextCompletionKernel(semanticKernel, "text");

        await promptKernel.SendAsync(new SubmitCode("""
         #!function summarize --skill writer
         {{$input}}
         Summarize the text above.
         """));

        var result = await textKernel.SendAsync(new SubmitCode("""
            #!prompt function.writer.summarize
            Can you make this text any shorter?
            """));

        result.Events.Should().NotContainErrors();

        result.Events.Should().ContainSingle<ReturnValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(f => f.MimeType == "text/plain")
              .Which
              .Value
              .Should()
              .Be("""
                    [text] Can you make this text any shorter?
                    Summarize the text above.
                    """);
    }

    [Fact]
    public async Task Can_use_prompts_with_context_values()
    {
        var semanticKernel = KernelBuilder.BuildSemanticKernel();

        var promptKernel = new PromptKernel(semanticKernel, "prompt");
        var textKernel = new TextCompletionKernel(semanticKernel, "text").UseValueSharing();

        await promptKernel.SendAsync(new SubmitCode("""
         #!function summarize --skill writer
         {{$input}}
         Summarize the text above in the style of {{$style}}.
         """));

        var result = await textKernel.SendAsync(new SubmitCode("""
            #!prompt function.writer.summarize
            #!set --name style --value "Boy George"
            Can you make this text any shorter?
            """));

        result.Events.Should().NotContainErrors();

        result.Events.Should().ContainSingle<ReturnValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(f => f.MimeType == "text/plain")
              .Which
              .Value
              .Should()
              .Be("""
                    [text] Can you make this text any shorter?
                    Summarize the text above in the style of Boy George.
                    """);
    }
}