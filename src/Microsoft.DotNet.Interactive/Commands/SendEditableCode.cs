// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands;

public class SendEditableCode : KernelCommand
{
    public SendEditableCode(string kernelName, string code, string targetKernelName = null)
        : base(targetKernelName)
    {
        KernelName = kernelName;
        Code = code;
    }

    public string KernelName { get; }

    public string Code { get; }
}