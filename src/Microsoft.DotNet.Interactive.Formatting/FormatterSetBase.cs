// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal abstract class FormatterSetBase : IFormatterSet
    {
        private Func<Type, ITypeFormatter> _factory = type => null;

        protected FormatterSetBase(
            ConcurrentDictionary<Type, ITypeFormatter> formatters = null)
        {
            Formatters = formatters ??
                         new ConcurrentDictionary<Type, ITypeFormatter>();
        }

        protected ConcurrentDictionary<Type, ITypeFormatter> Formatters { get; }

        public void AddFormatterFactory(Func<Type, ITypeFormatter> factory)
        {
            var previousFactory = _factory;
            _factory = t => factory(t) ?? previousFactory(t);
        }

        public bool TryGetFormatterForType(Type type, out ITypeFormatter formatter)
        {
            formatter = _factory(type);

            if (formatter != null)
            {
                return true;
            }

            if (Formatters.TryGetValue(type, out formatter))
            {
                return true;
            }

            if (TryInferFormatter(
                    type,
                    out formatter))
            {
                Formatters[type] = formatter;
                return true;
            }

            return false;
        }

        protected abstract bool TryInferFormatter(Type type, out ITypeFormatter formatter);
    }
}