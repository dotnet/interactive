// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Notebook
{
    public class InputCellMetadata
    {
        [JsonProperty("language")]
        public string Language { get; set; }
    }
}
