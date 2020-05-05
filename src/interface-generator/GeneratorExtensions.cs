﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;

namespace Microsoft.DotNet.Interactive
{
    public static class GeneratorExtensions
    {
        public static Type EffectiveBaseType(this Type type)
        {
            var baseType = type.BaseType;
            if (baseType == typeof(object) || baseType == typeof(ValueType))
            {
                // don't navigate up to these types
                return null;
            }

            return baseType;
        }

        public static string CamlCase(this string value)
        {
            return char.ToLower(value[0]) + value.Substring(1);
        }

        public static bool IsNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public static bool ShouldBeArray(this Type type)
        {
            return type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type);
        }
    }
}
