// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestInput : KernelCommand
{
    public RequestInput(
        string prompt,
        string targetKernelName = null,
        string inputTypeHint = null,
        string valueName = null)
        : base(targetKernelName)
    {
        ValueName = valueName;
        Prompt = prompt;
        InputTypeHint = inputTypeHint ?? "text";
        IsPassword = InputTypeHint == "password";
    }

    public string Prompt { get; }

    public bool IsPassword { get; }

    public string InputTypeHint { get; }

    public string ValueName { get; }
}