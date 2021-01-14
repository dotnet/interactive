// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Notebook;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public static class JupyterRequestContextExtensions
    {
        public static string GetLanguage(this JupyterRequestContext context)
        {
            if (context.JupyterRequestMessageEnvelope.MetaData.TryGetValue(NotebookFileFormatHandler.MetadataNamespace, out var candidateMetadata) &&
                candidateMetadata is JObject dotnetMetadata)
            {
                return dotnetMetadata["language"]?.ToObject<string>();
            }

            return null;
        }
    }
}
