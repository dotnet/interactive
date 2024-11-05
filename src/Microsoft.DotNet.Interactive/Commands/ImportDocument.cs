// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Commands;

public class ImportDocument : KernelCommand
{
    public ImportDocument(
        string filePath,
        string targetKernelName = null) : base(targetKernelName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(filePath));
        }

        FilePath = filePath;
    }

    [JsonPropertyName("file")] public string FilePath { get; }
}