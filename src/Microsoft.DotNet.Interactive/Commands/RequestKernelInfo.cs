// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestKernelInfo : KernelCommand
{
    public RequestKernelInfo(
        string targetKernelName = null,
        Uri destinationUri = null) : base(targetKernelName)
    {
        DestinationUri = destinationUri;
    }
}