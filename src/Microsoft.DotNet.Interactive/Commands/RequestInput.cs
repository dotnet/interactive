// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestInput : KernelCommand
{
    [JsonConstructor]
    public RequestInput(
        string prompt,
        string targetKernelName = null,
        string inputTypeHint = null,
        string valueName = null)
        : base(targetKernelName)
    {
        ValueName = valueName;
        Prompt = prompt;
        InputTypeHint = inputTypeHint;
    }

    public string Prompt { get; }

    public bool IsPassword => InputTypeHint == "password";

    [JsonPropertyName("type")] 
    public string InputTypeHint { get; set; }

    public string ValueName { get; }

    public bool Save { get; set; }
}