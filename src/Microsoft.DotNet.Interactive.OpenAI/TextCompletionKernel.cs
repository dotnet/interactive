// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class TextCompletionKernel : OpenAIKernel
{
    public TextCompletionKernel(IKernel semanticKernel,
        string name) : base(semanticKernel, name, SubmissionHandlingType.TextCompletion)
    {
        
    }

    protected override async Task HandleSubmitCode(SubmitCode submitCode, KernelInvocationContext context)
    {
        var semanticFunction = SemanticKernel.CreateSemanticFunction("{{$INPUT}}");

        var semanticKernelResponse = await SemanticKernel.RunAsync(
            submitCode.Code,
            context.CancellationToken,
            semanticFunction);

        var plainTextValue = new FormattedValue(PlainTextSummaryFormatter.MimeType, semanticKernelResponse.Result.ToDisplayString(PlainTextSummaryFormatter.MimeType));

        var htmlValue = new FormattedValue(HtmlFormatter.MimeType, semanticKernelResponse.ToDisplayString(HtmlFormatter.MimeType));

        var formattedValues = new[]
        {
            plainTextValue,
            htmlValue
        };

        context.Publish(new ReturnValueProduced(semanticKernelResponse, submitCode, formattedValues));
     
    }
}