// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter;

internal class KernelSpec
{
    [JsonPropertyName("argv")]
    public IReadOnlyList<string> CommandArguments { get; set; }

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, object> Metadata { get; set; }
}
