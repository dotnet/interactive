// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.CSharp;

public static class CSharpKernelExtensions
{
    public static CSharpKernel UseKernelHelpers(
        this CSharpKernel kernel)
    {
        var command = new SubmitCode($@"
using static {typeof(Kernel).FullName};
");

        kernel.DeferCommand(command);

        return kernel;
    }
}