// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.AI.TextCompletion;

namespace Microsoft.DotNet.Interactive.OpenAI.Tests;

internal class MockTextCompletion : ITextCompletion
{
    public Task<string> CompleteAsync(
        string text,
        CompleteRequestSettings requestSettings,
        CancellationToken cancellationToken = new())
    {
        return Task.FromResult($"[text] {text}");
    }
}