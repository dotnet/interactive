﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.App.CommandLine;

public class KernelHttpOptions
{
    public KernelHttpOptions(string defaultKernel)
    {
        DefaultKernel = defaultKernel;
    }

    public string DefaultKernel { get; }
}