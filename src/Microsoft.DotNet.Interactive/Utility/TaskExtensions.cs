// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Utility;

internal static class TaskExtensions
{
    internal static void WaitAndUnwrapException(this Task task) =>
        task?.GetAwaiter().GetResult();
}