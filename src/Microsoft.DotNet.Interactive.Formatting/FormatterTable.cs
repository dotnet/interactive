// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Reflection;

#nullable enable

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class FormatterTable
    {
        private readonly ConcurrentDictionary<(Type type, bool flag), ITypeFormatter> _formatters = new ConcurrentDictionary<(Type type, bool flag), ITypeFormatter>();
        private readonly Type _genericDef;
        private readonly string _name;

        internal FormatterTable(Type genericDef, string name)
        {
            _genericDef = genericDef;
            _name = name;
        }

        internal ITypeFormatter GetFormatter(Type type, bool flag)
        {
            return
                _formatters.GetOrAdd((type, flag),
                                     tup =>
                                     {
                                         return
                                             (ITypeFormatter)
                                             _genericDef
                                                 .MakeGenericType(tup.type)
                                                 .GetMethod(_name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                                                 .Invoke(null, new object[] { flag });
                                     });
        }
    }
}