// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public class KernelSpec
    {
        [JsonPropertyName("argv")]
        public string[] Arg { get; }

        [JsonPropertyName("display_name")]
        public string DisplayName { get; }

        [JsonPropertyName("language")]
        public string Language { get; }

        [JsonPropertyName("interrupt_mode ")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string InterruptMode { get; }

        [JsonPropertyName("metadata")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, object> MetaData { get; }

        [JsonPropertyName("env ")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public IReadOnlyDictionary<string, object> Env { get; }

        public KernelSpec(string[] arg, string displayName, string language, IReadOnlyDictionary<string, object> env = null, IReadOnlyDictionary<string, object> metaData = null, string interruptMode = null)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(language))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(language));
            }

            Arg = arg ?? throw new ArgumentNullException(nameof(arg));
            DisplayName = displayName;
            Language = language;
            Env = env;
            MetaData = metaData;
            InterruptMode = interruptMode;
        }
    }
}