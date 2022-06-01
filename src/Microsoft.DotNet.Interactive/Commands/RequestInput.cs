// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestInput : KernelCommand
{
    public RequestInput(string prompt, bool isPassword = false, string targetKernelName = null, string inputType = null)
        : base(targetKernelName)
    {
        Prompt = prompt;
        IsPassword = isPassword;
        InputType = inputType;
    }

    public string Prompt { get; }

    public bool IsPassword { get; }

    public string InputType { get; }
}