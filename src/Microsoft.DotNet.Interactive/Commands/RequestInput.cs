// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestInput : KernelCommand
{
    public RequestInput(string inputName, string prompt, bool isPassword = false, string targetKernelName = null)
        : base(targetKernelName)
    {
        InputName = inputName;
        Prompt = prompt;
        IsPassword = isPassword;
    }

    public string InputName { get; }
    public string Prompt { get; }

    public bool IsPassword { get; }
}