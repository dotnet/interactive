﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class Destructurer<T> :
        IDestructurer<T>,
        IDestructurer
    {
        private static readonly object _lockObj = new();
        private static IDictionary<string, Func<T, object>> _getters;

        internal readonly IDictionary<string, Func<T, object>> _instanceGetters;

        public Destructurer()
        {
            EnsureInitialized();

            _instanceGetters = new Dictionary<string, Func<T, object>>(StringComparer.OrdinalIgnoreCase)
                .Merge(_getters,
                       comparer: StringComparer.OrdinalIgnoreCase);
        }

        private static void EnsureInitialized()
        {
            if (_getters is not null)
            {
                return;
            }

            lock (_lockObj)
            {
                // while the double-checked lock is not 100% reliable, multiple initialization is safe in this case. the static setters are not modified (per T) after initialization, so this is a performance optimization to avoid taking locks during read operations.
                if (_getters is not null)
                {
                    return;
                }

                var members = typeof(T).GetMembersToFormat();

                _getters = members
                    .ToDictionary(member => member.Name,
                                  member => new Getter(member).GetValue,
                                  StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Gets the value of the specified property from an instance of <typeparamref name="T" />.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="propertyName">The name of the property whose value is to be returned.</param>
        /// <returns></returns>
        public object GetValue(T instance, string propertyName)
        {
            return _instanceGetters[propertyName](instance);
        }

        /// <summary>
        ///   Decomposes the specified instance into a sequence of key value pairs.
        /// </summary>
        /// <param name = "instance">The instance to be decomposed.</param>
        /// <returns>A sequence of key value pairs, where the keys are the member names and the values are the member values from the source instance.</returns>
        public IDictionary<string, object> Destructure(T instance)
        {
            var dictionary = _instanceGetters.ToDictionary(p => p.Key, p => p.Value(instance));

            return dictionary;
        }

        internal class Getter
        {
            public Getter(MemberInfo member)
            {
                Member = member;
                var source = Expression.Parameter(typeof(T), "source");

                GetValue = (Func<T, object>) Expression.Lambda(
                    typeof(Func<T, object>),
                    Expression.TypeAs(
                        Expression.PropertyOrField(source, Member.Name),
                        typeof(object)),
                    source).Compile();
            }

            public MemberInfo Member { get; }

            public Func<T, object> GetValue { get; set; }
        }

        IDictionary<string, object> IDestructurer.Destructure(object instance)
        {
            return Destructure((T) instance);
        }
    }
}