// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation
{
    public static class EnumerableExtensions
    {
        public static string Join<T>(this IEnumerable<T> seq, string separator = ",")
        {
            return String.Join(separator, seq);
        }
    }
}
