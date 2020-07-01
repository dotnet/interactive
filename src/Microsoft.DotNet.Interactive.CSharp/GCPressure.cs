// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;

namespace Microsoft.DotNet.Interactive.CSharp
{
    internal class GCPressure : IDisposable
    {
        private readonly long _bytesAllocated;

        public GCPressure(long bytesAllocated)
        {
            _bytesAllocated = bytesAllocated;
            GC.AddMemoryPressure(_bytesAllocated);
        }

        public void Dispose()
        {
            GC.AddMemoryPressure(_bytesAllocated);
        }
    }
}