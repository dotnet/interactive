// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public abstract class FormatterTestBase : IDisposable
    {
        private static readonly object _lock = new object();

        protected FormatterTestBase()
        {
            Monitor.Enter(_lock);

            Formatter.ResetToDefault();
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();

            Monitor.Exit(_lock);
        }
    }
}