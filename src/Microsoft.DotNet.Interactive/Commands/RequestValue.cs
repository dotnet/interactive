﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestValue : KernelCommand
{
    public RequestValue(
        string name, 
        string mimeType = "application/json", 
        string targetKernelName = null) : base(targetKernelName)
    {
        Name = name;
        MimeType = mimeType ?? "application/json";
    }

    public string Name { get; }

    public string MimeType { get; }
}