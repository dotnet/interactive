// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal static class Destructurer
    {
        private static ConcurrentDictionary<Type, IDestructurer> _cache;

        static Destructurer()
        {
            InitializeCache();
            Formatter.Clearing += (sender, args) => InitializeCache();
        }

        private static void InitializeCache()
        {
            _cache = new ConcurrentDictionary<Type, IDestructurer>();
        }

        public static IDestructurer GetOrCreate(Type type)
        {
            if (type == null)
            {
                return NonDestructurer.Instance;
            }

            return _cache.GetOrAdd(type, t =>
            {
                if (t.IsScalar())
                {
                    return NonDestructurer.Instance;
                }

                if (typeof(Type).IsAssignableFrom(t))
                {
                    return NonDestructurer.Instance;
                }

                return (IDestructurer) Activator.CreateInstance(typeof(Destructurer<>).MakeGenericType(t));
            });
        }
    }
}