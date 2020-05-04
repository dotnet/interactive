// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.Server
{
    public interface IKernelCommandEnvelope
    {
        IKernelCommand Command { get; }

        string CommandType { get; }

        string Token { get; }
    }
}