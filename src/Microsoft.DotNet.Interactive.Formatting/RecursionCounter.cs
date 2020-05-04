// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using Pocket;

namespace Microsoft.DotNet.Interactive.Formatting
{
    [DebuggerStepThrough]
    internal class RecursionCounter
    {
        private int _depth = 0;

        public int Depth => _depth;

        public IDisposable Enter()
        {
            _depth += 1;
            return Disposable.Create(() => _depth -= 1);
        }
    }
}