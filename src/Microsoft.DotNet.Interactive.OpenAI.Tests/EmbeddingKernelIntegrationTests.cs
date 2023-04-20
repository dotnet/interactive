// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.OpenAI.Configuration;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.SemanticKernel.Orchestration;

namespace Microsoft.DotNet.Interactive.OpenAI.Tests;

public class EmbeddingKernelIntegrationTests
{
    private readonly SemanticKernelSettings _settings;

    public EmbeddingKernelIntegrationTests()
    {
        if (!SemanticKernelSettings.TryLoadFromFile(
                AIIngregrationTestFactAttribute.ConfigFilePath(),
                out _settings))
        {
            throw new Exception("Tests configuration is invalid");
        }
    }

    [AIIngregrationTestFact]
    public async Task Embeddings_are_used_when_the_context_is_declared()
    {
        using var root = new CompositeKernel();

        foreach (var kernel in OpenAIKernelConnector.CreateKernels(_settings, "openai"))
        {
            root.Add(kernel);
        }

        var embeddingsKernel = root.ChildKernels.OfType<TextEmbeddingGenerationKernel>().Single();

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact1
            .NET Interactive is a polyglot kernel.
            """));

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact2
            .NET Interactive is developed by a team of engineers at Microsoft.
            """));

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact3
            .NET Interactive is an open source project with a lot of contributors.
            """));

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact4
            .NET Interactive is used by engineers.
            """));


        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact5
            .NET Interactive is developed by open source community.
            """));

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact6
            .NET Interactive is used by software engineers.
            """));

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact7
            .NET Interactive can create rich outputs and visualizations.
            """));

        var textCompletionKernel = root.ChildKernels.OfType<TextCompletionKernel>().Single();

        var result = await textCompletionKernel.SendAsync(new SubmitCode("""
            #!conversation-context .NET Interactive open source
            Who is developing a polyglot kernel?
            """));

        var skContext = result.Events.Should().ContainSingle<ReturnValueProduced>()
                              .Which.Value.As<SKContext>();

        skContext.Result.Should().Contain("Microsoft").And.Contain("community");
    }

    [AIIngregrationTestFact]
    public async Task Embeddings_can_be_used_with_pipeline()
    {
        using var root = new CompositeKernel();

        foreach (var kernel in OpenAIKernelConnector.CreateKernels(_settings, "openai"))
        {
            root.Add(kernel);
        }

        var embeddingsKernel = root.ChildKernels.OfType<TextEmbeddingGenerationKernel>().Single();

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact1
            .NET Interactive is a polyglot kernel.
            """));

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact2
            .NET Interactive is developed by a team of engineers at Microsoft.
            """));

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact3
            .NET Interactive is an open source project with a lot of contributors.
            """));

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact4
            .NET Interactive is used by engineers.
            """));


        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact5
            .NET Interactive is developed by open source community.
            """));

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact6
            .NET Interactive is used by software engineers.
            """));

        await root.SendAsync(new SubmitCode($"""
            #!{embeddingsKernel.Name} --name fact7
            .NET Interactive can create rich outputs and visualizations.
            """));

        var skillKernel = root.ChildKernels.OfType<SkillKernel>().Single();

        await skillKernel.SendAsync(new SubmitCode(""""
            #!function summarize --skill writer

            {{$input}}

            Summarize the text above in 10 words.

            """
            """"));

        var textCompletionKernel = root.ChildKernels.OfType<TextCompletionKernel>().Single();

        var result = await textCompletionKernel.SendAsync(new SubmitCode("""
            #!conversation-context .NET Interactive open source
            #!use-skills function.writer.summarize
            Who is developing a polyglot kernel?
            """));

        var skContext = result.Events.Should().ContainSingle<ReturnValueProduced>()
                              .Which.Value.As<SKContext>();

        skContext.Result.Should().Contain("Microsoft");
    }
}