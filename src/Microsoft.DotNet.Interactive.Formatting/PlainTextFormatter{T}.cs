// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class PlainTextFormatter<T> : TypeFormatter<T>
    {
        private readonly Action<T, TextWriter> _format;

        protected PlainTextFormatter()
        {
            _format = WriteDefault;
        }

        public PlainTextFormatter(Action<T, TextWriter> format)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format));
        }

        public static ITypeFormatter<T> Create(bool includeInternals = false)
        {
            if (PlainTextFormatter.DefaultFormatters.TryGetFormatterForType(typeof(T), out var formatter) &&
                formatter is ITypeFormatter<T> ft)
            {
                return ft;
            }

            if (Formatter<T>.TypeIsAnonymous ||
                Formatter<T>.TypeIsException ||
                Formatter<T>.TypeIsValueTuple|| 
                !typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                return CreateForAllMembers(includeInternals);
            }

            return Default;
        }

        public override string MimeType => PlainTextFormatter.MimeType;

        public override void Format(T instance, TextWriter writer)
        {
            _format(instance, writer);
        }

        private static PlainTextFormatter<T> CreateForAllMembers(bool includeInternals = false)
        {
            if (typeof(T).IsScalar())
            {
                return new PlainTextFormatter<T>((value, writer) => writer.Write(value));
            }

            return new PlainTextFormatter<T>(
                PlainTextFormatter.CreateFormatDelegate<T>(
                    typeof(T).GetAllMembers(includeInternals).ToArray()));
        }

        public static PlainTextFormatter<T> CreateForMembers(params Expression<Func<T, object>>[] members)
        {
            var format = PlainTextFormatter.CreateFormatDelegate<T>(
                typeof(T).GetMembers(members).ToArray());

            return new PlainTextFormatter<T>(format);
        }

        public static PlainTextFormatter<T> Default { get; } = new PlainTextFormatter<T>();

        internal virtual void WriteDefault(
            T obj,
            TextWriter writer)
        {
            if (obj is string)
            {
                writer.Write(obj);
                return;
            }

            switch (obj)
            {
                case IEnumerable enumerable:
                    Formatter.Join(
                        enumerable,
                        writer,
                        Formatter<T>.ListExpansionLimit);
                    break;
                default:
                    writer.Write(obj.ToString());
                    break;
            }
        }
    }
}