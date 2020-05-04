// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Server
{
    public interface IKernelEventEnvelope
    {
        IKernelEvent Event { get; }

        string EventType { get; }
    }
}