// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
namespace Microsoft.DotNet.Interactive.Directives;

public class KernelSpecifierDirective : KernelDirective
{
    public KernelSpecifierDirective(string name, string kernelName) : base(name)
    {
        KernelName = kernelName;
    }

    public string KernelName { get; }
}