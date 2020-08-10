// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Utility
{
    public class GCPressure : IDisposable
    {
        private readonly long _bytesAllocated;

        public GCPressure(long bytesAllocated)
        {
            _bytesAllocated = bytesAllocated;
            GC.AddMemoryPressure(_bytesAllocated);
        }

        public void Dispose()
        {
            GC.RemoveMemoryPressure(_bytesAllocated);
        }
    }
}