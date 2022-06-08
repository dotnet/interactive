// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Pocket;

#nullable enable

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class FormatterMapByType
    {
        private readonly ConcurrentDictionary<(Type type, bool flag), ITypeFormatter> _formatters = new();
        private readonly Type _genericDef;
        private readonly string _name;

        internal FormatterMapByType(Type genericDef, string name)
        {
            _genericDef = genericDef;
            _name = name;
        }

        internal ITypeFormatter GetOrCreateFormatterForType(Type type, bool includeInternals)
        {
            using var _ = Disposable.Create(() => Console.WriteLine($"Leaving FormatterMapByType.{nameof(GetOrCreateFormatterForType)} for type {type.Name}"));
            Console.WriteLine($"Entering FormatterMapByType.{nameof(GetOrCreateFormatterForType)} for type {type.Name}");

            return
                _formatters.GetOrAdd((type, includeInternals),
                                     tup =>
                                     {
                                         using var _ = Disposable.Create(() => Console.WriteLine($"Leaving FormatterMapByType.{nameof(GetOrCreateFormatterForType)}.Add for key ({tup.type.Name},{tup.flag})"));
                                         Console.WriteLine($"Entering FormatterMapByType.{nameof(GetOrCreateFormatterForType)}.Add for for key ({tup.type.Name},{tup.flag})");
                                         return
                                             (ITypeFormatter)
                                             _genericDef
                                                 .MakeGenericType(tup.type)
                                                 .GetMethod(_name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                                                 .Invoke(null, new object[] { includeInternals });
                                     });
        }
    }
}