// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Commands;

public class RequestKernelInfo : KernelCommand
{
    [JsonConstructor]
    public RequestKernelInfo(string targetKernelName = null) : base(targetKernelName)
    {
    }

    public RequestKernelInfo(Uri destinationUri)
    {
        DestinationUri = destinationUri;
    }
}