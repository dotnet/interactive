// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Documents.Jupyter;

namespace Microsoft.DotNet.Interactive.Jupyter;

public static class JupyterRequestContextExtensions
{
    public static string GetKernelName(this JupyterRequestContext context)
    {
        string kernelName = null;
        if (context.JupyterRequestMessageEnvelope.MetaData.TryGetValue(
                "dotnet_interactive", 
                out var candidateDotnetMetadata) &&
            candidateDotnetMetadata is InputCellMetadata dotnetMetadata)
        {
            kernelName = dotnetMetadata.Language;
        }

        if (context.JupyterRequestMessageEnvelope.MetaData.TryGetValue(
                "polyglot_notebook", 
                out var candidatePolyglotMetadata) &&
            candidatePolyglotMetadata is InputCellMetadata polyglotMetadata)
        {
            kernelName = polyglotMetadata.KernelName;
        }

        return kernelName;
    }
}