﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Documents.Jupyter
{

    public class InputCellMetadata
    {
        [JsonPropertyName("language")]
        public string Language { get; }

        public InputCellMetadata(string language = null)
        {
            Language = language;
        }
    }
}
