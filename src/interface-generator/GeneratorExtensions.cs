// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.DotNet.Interactive.InterfaceGen.App
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

        public static Type GetArrayElementType(this Type type)
        {
            return type.IsArray ? type.GetElementType() : type.GenericTypeArguments[0];
        }

        public static string CamelCase(this string value)
        {
            return char.ToLower(value[0]) + value.Substring(1);
        }

        public static bool IsNullable(this Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        public static bool ShouldBeArray(this Type type)
        {
            return type.IsArray 
                   || (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type));
        }

        public static bool ShouldBeDictionaryOfString(this Type type)
        {
            return type.IsGenericType
                && (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
                    type.GetGenericTypeDefinition() == typeof(Dictionary<,>) || type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>) ||
                    type.GetGenericTypeDefinition() == typeof(ReadOnlyDictionary<,>))
                && type.GenericTypeArguments[0] == typeof(string);
        }
    }
}
