// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.Utility;

public class AsyncLazy<T>
{
    private readonly Lazy<Task<T>> _lazy;

    public AsyncLazy(Func<Task<T>> initialize)
    {
        if (initialize == null)
        {
            throw new ArgumentNullException(nameof(initialize));
        }

        _lazy = new Lazy<Task<T>>(initialize);
    }

    public Task<T> ValueAsync() => _lazy.Value;
}