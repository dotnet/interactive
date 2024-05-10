// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

[Collection("Do not parallelize")]
public abstract class FormatterTestBase : IDisposable
{
    private static readonly object _lock = new();

    protected FormatterTestBase()
    {
        Monitor.Enter(_lock);

        Formatter.ResetToDefault();
    }

    public virtual void Dispose()
    {
        Formatter.ResetToDefault();

        Monitor.Exit(_lock);
    }
}