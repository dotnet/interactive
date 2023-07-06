// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.Interactive.Formatting;

internal static class TypeExtensions
{
    private const BindingFlags BindingFlagsForFormattedMembers = BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Public;

    private static readonly HashSet<Type> _typesToTreatAsScalar = new()
    {
        typeof(decimal),
        typeof(Guid),
        typeof(string),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
    };

    public static string MemberName<T, TValue>(this Expression<Func<T, TValue>> expression)
    {
        if (expression is null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        if (expression.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        // when the return type of the expression is a value type, it contains a call to Convert, resulting in boxing, so we get a UnaryExpression instead
        if (expression.Body is UnaryExpression unaryExpression)
        {
            memberExpression = unaryExpression.Operand as MemberExpression;
            if (memberExpression is not null)
            {
                return memberExpression.Member.Name;
            }
        }

        throw new ArgumentException($"Expression {expression} does not specify a member.");
    }

    public static MemberAccessor<T>[] GetMemberAccessors<T>(this IEnumerable<MemberInfo> forMembers) =>
        forMembers
            .Select(memberInfo => new MemberAccessor<T>(memberInfo))
            .ToArray();

    public static IEnumerable<MemberInfo> GetMembersToFormat(this Type type)
    {
        return type.GetMembers(BindingFlagsForFormattedMembers)
                   .Where(ShouldDisplay)
                   .ToArray();
    }

    public static bool IsRelevantFormatterFor(this Type type, Type actualType)
    {
        if (!type.IsGenericTypeDefinition)
        {
            return type.IsAssignableFrom(actualType);
        }

        var baseChain = actualType;

        while (baseChain is not null)
        {
            if (baseChain.IsGenericType && baseChain.GetGenericTypeDefinition() == type)
            {
                return true;
            }

            baseChain = baseChain.BaseType;
        }

        foreach (var i in actualType.GetInterfaces())
        {
            if (i.IsGenericType && i.GetGenericTypeDefinition() == type)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsAnonymous(this Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false) &&
               type.IsGenericType && type.Name.Contains("AnonymousType") &&
               (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"));
    }

    public static bool IsScalar(this Type type)
    {
        if (_typesToTreatAsScalar.Contains(type))
        {
            return true;
        }

        return type.IsPrimitive ||
               (type.IsConstructedGenericType &&
                type.GetGenericTypeDefinition() == typeof(Nullable<>) && 
                type.GetGenericArguments()[0].IsScalar());
    }

    public static bool IsValueTuple(this Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return type.ToString().StartsWith("System.ValueTuple`");
    }
        
    public static bool IsTuple(this Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        return type.ToString().StartsWith("System.Tuple`");
    }

    public static bool ShouldDisplay(MemberInfo m)
    {
        if (m.MemberType is not (MemberTypes.Property or MemberTypes.Field))
        {
            return false;
        }

        if (!(!m.Name.Contains("<") &&
              !m.Name.Contains("k__BackingField")))
        {
            return false;
        }

        if (m.MemberType is not MemberTypes.Property)
        {
            return true;
        }

        if (m is PropertyInfo property)
        {
            if (m.GetCustomAttribute<DebuggerBrowsableAttribute>() is {} b &&
                b.State == DebuggerBrowsableState.Never)
            {
                return false;
            }

            if (property.CanRead &&
                property.GetIndexParameters().Length == 0)
            {
                return true;
            }
        }

        return false;
    }

    internal static Type GetElementTypeIfEnumerable(this Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        Type enumerableInterface;

        if (type.IsEnumerable())
        {
            enumerableInterface = type;
        }
        else
        {
            enumerableInterface = type
                .GetInterfaces()
                .FirstOrDefault(IsEnumerable);
        }

        if (enumerableInterface is null)
        {
            return null;
        }

        if (enumerableInterface.GenericTypeArguments is { Length: 1 } genericTypeArguments)
        {
            return genericTypeArguments[0];
        }

        var enumerableTInterfaces = enumerableInterface
                                    .GetInterfaces()
                                    .Where(i => i.IsGenericType)
                                    .Where(i => i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                                    .ToArray();

        if (enumerableTInterfaces is { Length: 1 } x)
        {
            return x[0].GenericTypeArguments[0];
        }

        return null;
    }

    internal static bool IsEnumerable(this Type type)
    {
        if (type == typeof(string))
        {
            return false;
        }
            
        return
            type.IsArray
            ||
            typeof(IEnumerable).IsAssignableFrom(type);
    }

    public static bool IsDictionary(
        this Type type,
        out Func<object, IEnumerable> getKeys,
        out Func<object, IEnumerable> getValues,
        out Type keyType,
        out Type valueType)
    {
        var dictType =
            type.GetTypeInfo().ImplementedInterfaces
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
            ??
            type.GetTypeInfo().ImplementedInterfaces
                .FirstOrDefault(i => i == typeof(IDictionary));

        if (dictType is null)
        {
            getKeys = null;
            getValues = null;
            keyType = null;
            valueType = null;

            return false;
        }

        var keysProperty = dictType.GetProperty("Keys");
        getKeys = instance => (IEnumerable)keysProperty.GetValue(instance, null);

        var valuesProperty = dictType.GetProperty("Values");
        getValues = instance => (IEnumerable)valuesProperty.GetValue(instance, null);

        if (type.GetElementTypeIfEnumerable() is { } keyValuePairType &&
            keyValuePairType.IsConstructedGenericType &&
            keyValuePairType.GetGenericTypeDefinition() is { } genericTypeDefinition &&
            genericTypeDefinition == typeof(KeyValuePair<,>))
        {
            keyType = keyValuePairType.GetGenericArguments()[0];
            valueType = keyValuePairType.GetGenericArguments()[1];
        }
        else
        {
            keyType = null;

            valueType = null;
        }

        return true;
    }
}