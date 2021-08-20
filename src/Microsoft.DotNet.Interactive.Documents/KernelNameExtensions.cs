// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Documents
{
    internal static class KernelNameExtensions
    {
        public static IDictionary<string, string> ToMapOfKernelNamesByAlias(this IEnumerable<KernelName> kernelNames)
        {
            return kernelNames
                .SelectMany(n => n.Aliases.Select(a => (name: n.Name, alias: a)))
                .ToDictionary(x => x.alias, x => x.name);
        }
    }
}