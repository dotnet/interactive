// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive
{
    internal static class TypeExtensions
    {
        internal static bool CanBeInstantiated(this Type type)
        {
            return !type.IsAbstract
                   && !type.IsGenericTypeDefinition
                   && !type.IsInterface;
        }
    }
}