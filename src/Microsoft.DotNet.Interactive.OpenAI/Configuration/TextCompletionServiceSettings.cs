// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.OpenAI.Configuration;

public class TextCompletionServiceSettings
{
    public string? Endpoint { get; set; }
    public string? ModelOrDeploymentName { get; set; }
    public string? ApiKey { get; set; }
    public string? OrgId { get; set; }
    public bool UseAzureOpenAI { get; set; }
}

public class TextEmbeddingGenerationServiceSettings
{
    public string? Endpoint { get; set; }
    public string? ModelOrDeploymentName { get; set; }
    public string? ApiKey { get; set; }
    public string? OrgId { get; set; }
    public bool UseAzureOpenAI { get; set; }
}

public class ChatCompletionServiceSettings
{
    public string? ModelOrDeploymentName { get; set; }
    public string? ApiKey { get; set; }
    public string? OrgId { get; set; }
}

public class ImageGenerationServiceSettings
{
    public string? ApiKey { get; set; }
    public string? OrgId { get; set; }
}