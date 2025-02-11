// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.App.Commands;

public class ExpandCode : KernelCommand
{
    public ExpandCode(string name, string targetKernelName = null) : base(targetKernelName)
    {
        Name = name;
    }

    public string Name { get; }

    public int? InsertAtPosition { get; set; }
}