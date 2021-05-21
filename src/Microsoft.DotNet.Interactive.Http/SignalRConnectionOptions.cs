// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Connection;

#nullable enable
namespace Microsoft.DotNet.Interactive.Http
{
    public class SignalRConnectionOptions : KernelConnectionOptions
    {
        public string? HubUrl { get; set; }
    }
}