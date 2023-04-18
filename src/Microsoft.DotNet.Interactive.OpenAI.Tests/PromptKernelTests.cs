// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.SemanticKernel.Orchestration;
using Xunit;

namespace Microsoft.DotNet.Interactive.OpenAI.Tests;

public class PromptKernelTests
{
    [Fact]
    public async Task Can_create_a_global_function()
    {
        var semanticKernel = KernelBuilder.BuildSemanticKernel();

        using var promptKernel = new PromptKernel(semanticKernel, "prompt");

        var result = await promptKernel.SendAsync(new SubmitCode("""
         #!function summarize

         {{$input}}

         Summarize the text above.

         """));

        result.Events.Should().NotContainErrors();

        var function = semanticKernel.Skills.GetFunction("summarize");

        function.Name.Should().Be("summarize");
        function.SkillName.Should().Be("_GLOBAL_FUNCTIONS_");
    }

    [Fact]
    public async Task Can_redefine_a_global_function()
    {
        var semanticKernel = KernelBuilder.BuildSemanticKernel();

        using var promptKernel = new PromptKernel(semanticKernel, "prompt");

        await promptKernel.SendAsync(new SubmitCode("""
         #!function summarize

         {{$input}}

         Summarize the text above.

         """));

        var result = await promptKernel.SendAsync(new SubmitCode("""
         #!function summarize

         {{$input}}

         Summarize the text above in the style of {{$person}}.

         """));

        result.Events.Should().NotContainErrors();

        var function = (SKFunction)semanticKernel.Skills.GetFunction("summarize");

        function.Parameters.Should().ContainSingle(p => p.Name == "person");
    }

    [Fact]
    public async Task Can_create_a_skill_function()
    {
        var semanticKernel = KernelBuilder.BuildSemanticKernel();

        using var promptKernel = new PromptKernel(semanticKernel, "prompt");

        var result = await promptKernel.SendAsync(new SubmitCode("""
         #!function summarize --skill writer

         {{$input}}

         Summarize the text above.

         """));

        result.Events.Should().NotContainErrors();

        var function = semanticKernel.Skills.GetFunction("writer", "summarize");

        function.Name.Should().Be("summarize");
        function.SkillName.Should().Be("writer");
    }

    [Fact]
    public async Task Functions_are_visible_as_kernel_values()
    {
        var semanticKernel = KernelBuilder.BuildSemanticKernel();

        using var promptKernel = new PromptKernel(semanticKernel, "prompt");

        await promptKernel.SendAsync(new SubmitCode("""
         #!function summarize --skill writer
         {{$input}} 
         """));

        await promptKernel.SendAsync(new SubmitCode("""
         #!function shakespearianize --skill writer
         {{$input}} 
         """));

        var result = await promptKernel.SendAsync(new RequestValueInfos());

        result.Events
              .Should()
              .ContainSingle<ValueInfosProduced>()
              .Which
              .ValueInfos
              .Select(v => v.Name)
              .Should()
              .BeEquivalentTo("function.writer.summarize", "function.writer.shakespearianize");
    }

    [Fact]
    public async Task Function_magic_values_are_not_persistent()
    {
        var semanticKernel = KernelBuilder.BuildSemanticKernel();

        using var promptKernel = new PromptKernel(semanticKernel, "prompt");

        await promptKernel.SendAsync(new SubmitCode("""
         #!function summarize --skill writer
         {{$input}} 
         """));

        await promptKernel.SendAsync(new SubmitCode("""
         #!function shakespearianize 
         {{$input}} 
         """));

        var result = await promptKernel.SendAsync(new RequestValueInfos());

        result.Events
              .Should()
              .ContainSingle<ValueInfosProduced>()
              .Which
              .ValueInfos
              .Select(v => v.Name)
              .Should()
              .BeEquivalentTo("function.writer.summarize", "function._GLOBAL_FUNCTIONS_.shakespearianize");
    }
}