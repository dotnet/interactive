// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive
{
    internal class CommandEqualityComparer : IEqualityComparer<KernelCommand>
    {
        public static CommandEqualityComparer Instance { get; } = new();

        public bool Equals(KernelCommand x, KernelCommand y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x.Properties.TryGetValue(KernelCommandExtensions.IdKey, out var xId) &&
                xId is string xIdString && 
                y.Properties.TryGetValue(KernelCommandExtensions.IdKey, out var yId) &&
                yId is string yIdString )
            {
                return string.Equals(xIdString, yIdString, StringComparison.Ordinal);
            }

            return false;
        }

        public int GetHashCode(KernelCommand obj)
        {
            return obj.GetOrCreateId().GetHashCode();
        }
    }
}