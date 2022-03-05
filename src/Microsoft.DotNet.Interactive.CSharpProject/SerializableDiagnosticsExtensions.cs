// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.CSharpProject.Protocol
{
    internal static class SerializableDiagnosticsExtensions
    {
        public static IOrderedEnumerable<SerializableDiagnostic> Sort(this IEnumerable<SerializableDiagnostic> source) =>
            source.OrderBy(d => d?.BufferId?.ToString())
                  .ThenBy(d => d.Start)
                  .ThenBy(d => d.End);
    }
}