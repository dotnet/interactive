// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Invocation;

namespace Microsoft.DotNet.Interactive.Utility;

internal static class InvocationContextExtensions
{
    public static T GetService<T>(this InvocationContext context)
    {
        var service = context.BindingContext.GetService(typeof(T));

        if (service is null)
        {
            throw new ArgumentException($"Service not found: {typeof(T)}");
        }

        return (T)service;
    }
}