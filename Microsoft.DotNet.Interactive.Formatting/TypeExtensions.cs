// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal static partial class TypeExtensions
    {
        internal static bool CanBeInstantiated(this Type type)
        {
            return !type.IsAbstract
                   && !type.IsGenericTypeDefinition
                   && !type.IsInterface;
        }

        public static string MemberName<T, TValue>(this Expression<Func<T, TValue>> expression)
        {
            if (expression == null)
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
                if (memberExpression != null)
                {
                    return memberExpression.Member.Name;
                }
            }

            throw new ArgumentException($"Expression {expression} does not specify a member.");
        }

        public static IEnumerable<MemberInfo> GetMembers<T>(
            this Type type,
            params Expression<Func<T, object>>[] forProperties)
        {
            var allMembers = typeof(T).GetAllMembers(true).ToArray();

            if (forProperties == null || !forProperties.Any())
            {
                return allMembers;
            }

            return
                forProperties
                    .Select(p =>
                    {
                        var memberName = p.MemberName();
                        return allMembers.Single(m => m.Name == memberName);
                    });
        }

        public static MemberAccessor<T>[] GetMemberAccessors<T>(this IEnumerable<MemberInfo> forMembers) =>
            forMembers
                .Select(MemberAccessor.CreateMemberAccessor<T>)
                .ToArray();

        public static IEnumerable<MemberInfo> GetAllMembers(this Type type, bool includeInternals = false)
        {
            var bindingFlags = includeInternals
                                   ? BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Public | BindingFlags.NonPublic
                                   : BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.GetField | BindingFlags.Public;

            return type.GetMembers(bindingFlags)
                       .Where(m => !m.Name.Contains("<") && !m.Name.Contains("k__BackingField"))
                       .Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field)
                       .Where(m => m.MemberType != MemberTypes.Property ||
                                   ((PropertyInfo) m).CanRead && !((PropertyInfo) m).GetIndexParameters().Any())
                       .ToArray();
        }

        public static IEnumerable<Type> GetAllInterfaces(this Type type)
        {
            if (type.IsInterface)
            {
                yield return type;
            }
            foreach (var i in type.GetInterfaces())
                yield return i;
        }

        public static bool IsAnonymous(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false) &&
                   type.IsGenericType && type.Name.Contains("AnonymousType") &&
                   (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$")) &&
                   (type.Attributes & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        private static readonly HashSet<Type> _knownScalarTypes = new HashSet<Type>
        {
            typeof(decimal),
            typeof(Guid),
            typeof(string),
        };

        public static bool IsScalar(this Type type)
        {
            if (_knownScalarTypes.Contains(type))
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
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.ToString().StartsWith("System.ValueTuple`");
        }
        
        public static bool IsTuple(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.ToString().StartsWith("System.Tuple`");
        }
    }
}