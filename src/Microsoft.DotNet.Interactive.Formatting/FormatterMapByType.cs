﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Formatting;

internal class FormatterMapByType
{
    private readonly ConcurrentDictionary<Type, ITypeFormatter> _formatters = new();
    private readonly Type _genericDef;
    private readonly string _name;

    internal FormatterMapByType(Type genericDef, string name)
    {
        _genericDef = genericDef;
        _name = name;
    }

    internal ITypeFormatter GetOrCreateFormatterForType(Type type) =>
        _formatters.GetOrAdd(
            type,
            t => (ITypeFormatter)
                _genericDef
                    .MakeGenericType(t)
                    .GetMethod(_name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, Array.Empty<object>()));
}