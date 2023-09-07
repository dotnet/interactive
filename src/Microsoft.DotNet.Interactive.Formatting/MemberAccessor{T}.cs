// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Formatting;

internal class MemberAccessor<T> 
{
    private readonly MemberInfo _member;

    private readonly Func<T, object> _getter;

    public MemberAccessor(MemberInfo member)
    {
        _member = member;

        try
        {
            var targetParam = Expression.Parameter(typeof(T), "target");

            var propertyOrField = Expression.PropertyOrField(
                targetParam,
                _member.Name);

            var unaryExpression = Expression.TypeAs(
                propertyOrField,
                typeof(object));

            var lambdaExpression = Expression.Lambda<Func<T, object>>(
                unaryExpression,
                targetParam);

            _getter = lambdaExpression.Compile();
        }
        catch (Exception)
        {
            _getter = obj =>
            {
                if (obj is null)
                {
                    return Formatter.NullString;
                }

                return obj.ToString();
            };
        }
    }

    public string MemberName => _member.Name;

    public object GetValueOrException(T instance)
    {
        try
        {
            return _getter(instance);
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}