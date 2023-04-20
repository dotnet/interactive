// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
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
    public async Task Embeddings_are_retrieved_implicitly_when_something_something()
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
            .NET Interactive is developed by a team of engineers at Microsoft and open source contributors.
            """));

        var textCompletionKernel = root.ChildKernels.OfType<TextCompletionKernel>().Single();

        var result = await textCompletionKernel.SendAsync(new SubmitCode("""
            Who is developing a polyglot kernel?
            """));

        var skContext = result.Events.Should().ContainSingle<ReturnValueProduced>()
                              .Which.Value.As<SKContext>();

        var variables = skContext.Variables;

        variables.ContainsKey("fact1").Should().BeTrue();
        variables.ContainsKey("fact2").Should().BeTrue();




        // TODO (testname) write test
        throw new NotImplementedException();
    }
}