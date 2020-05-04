// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class MemberAccessor<T>
    {
        public MemberAccessor(MemberInfo member)
        {
            Member = member;

            try
            {
                var targetParam = Expression.Parameter(typeof(T), "target");

                var propertyOrField = Expression.PropertyOrField(
                    targetParam,
                    Member.Name);

                var unaryExpression = Expression.TypeAs(
                    propertyOrField,
                    typeof(object));

                var lambdaExpression = Expression.Lambda<Func<T, object>>(
                    unaryExpression,
                    targetParam);

                GetValue = lambdaExpression.Compile();
            }
            catch (Exception)
            {
                GetValue = obj =>
                {
                    if (obj == null)
                    {
                        return Formatter.NullString;
                    }

                    return obj.ToString();
                };
            }
        }

        public MemberInfo Member { get; }

        public bool Ignore { get; set; }

        public Func<T, object> GetValue { get; set; }
    }
}