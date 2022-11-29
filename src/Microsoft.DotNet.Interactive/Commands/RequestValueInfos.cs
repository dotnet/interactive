﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestValueInfos : KernelCommand
{
    public RequestValueInfos(string targetKernelName = null) : base(targetKernelName)
    {
    }
}