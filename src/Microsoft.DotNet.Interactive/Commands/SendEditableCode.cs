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

        if (KernelInvocationContext.Current is { Command: SubmitCode submitCode })
        {
            if (submitCode.Parameters.TryGetValue("cellIndex", out var cellIndexString) &&
                int.TryParse(cellIndexString, out var cellIndex))
            {
                InsertAtPosition = cellIndex + 1;
            }
        }
    }

    public string KernelName { get; }

    public string Code { get; }

    public int? InsertAtPosition { get; set; }
}