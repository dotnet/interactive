// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ImageGeneration;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class ImageGenerationKernel :
    Kernel,
    IKernelCommandHandler<SubmitCode>
{
    private IImageGeneration? _imageGenerationService;

    public ImageGenerationKernel(
        IKernel semanticKernel,
        string name) : base($"{name}(image)")
    {
        SemanticKernel = semanticKernel;
        KernelInfo.LanguageName = "text";
        KernelInfo.DisplayName = Name;
        KernelInfo.DisplayName = $"{Name} - DALL-E";
    }

    public IKernel SemanticKernel { get; }

    async Task IKernelCommandHandler<SubmitCode>.HandleAsync(SubmitCode submitCode, KernelInvocationContext context)
    {
        _imageGenerationService ??= SemanticKernel.GetService<IImageGeneration>();

        var height = 256;
        var width = 256;
        var imageUrl = await _imageGenerationService.GenerateImageAsync(
                           submitCode.Code,
                           width, height);

        var surface = await SkiaUtils.ShowImage(imageUrl, width, height);

        context.Publish(new ReturnValueProduced(
                            surface,
                            submitCode,
                            FormattedValue.CreateManyFromObject(surface)));
    }
}