// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal static class Destructurer
    {
        public static IDestructurer<T> Create<T>() => new Destructurer<T>();

        public static IDestructurer Create(Type type)
        {
            if (type.IsScalar())
            {
                return NonDestructurer.Instance;
            }

            return (IDestructurer) Activator.CreateInstance(typeof(Destructurer<>).MakeGenericType(type));
        }
    }
}