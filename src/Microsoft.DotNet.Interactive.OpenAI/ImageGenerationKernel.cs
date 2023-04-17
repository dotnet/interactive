// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ImageGeneration;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class ImageGenerationKernel : OpenAIKernel
{
    private IImageGeneration? _imageGenerationService;

    public ImageGenerationKernel(IKernel semanticKernel,
        string name) : base(semanticKernel, name, SubmissionHandlingType.ImageGeneration)
    {

    }

    protected override async Task HandleSubmitCode(SubmitCode submitCode, KernelInvocationContext context)
    {
        _imageGenerationService ??= SemanticKernel.GetService<IImageGeneration>();

        var height = 256;
        var width = 256;
        var imageUrl = await _imageGenerationService.GenerateImageAsync(
            submitCode.Code,
            width, height);

        await SkiaUtils.ShowImage(imageUrl, width, height);

    }
}