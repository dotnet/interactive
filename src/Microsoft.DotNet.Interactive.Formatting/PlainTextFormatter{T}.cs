// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public delegate bool FormatDelegate<in T>(T value, TextWriter writer, FormatContext context);

    public class PlainTextFormatter<T> : TypeFormatter<T>
    {
        private readonly FormatDelegate<T> _format;

        public PlainTextFormatter(FormatDelegate<T> format)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format));
        }

        public PlainTextFormatter(Action<T, TextWriter> format)
        {
            _format = FormatInstance;

            bool FormatInstance(T instance, TextWriter writer, FormatContext context)
            {
                format(instance, writer);
                return true;
            }
        }

        public PlainTextFormatter(Func<T, string> format)
        {
            _format = (instance, writer, context) =>
            {
                writer.Write(format(instance));
                return true;
            };
        }

        public override string MimeType => PlainTextFormatter.MimeType;

        public override bool Format(T value, TextWriter writer, FormatContext context)
        {
            if (value is null)
            {
                writer.Write(Formatter.NullString);
                return true;
            }

            return _format(value, writer, context);
        }

        public static PlainTextFormatter<T> CreateForAnyObject(bool includeInternals = false)
        {
            if (typeof(T).IsScalar())
            {
                return new PlainTextFormatter<T>((value, writer, context) =>
                {
                    writer.Write(value);
                    return true;
                });
            }

            return new PlainTextFormatter<T>(
                PlainTextFormatter.CreateFormatDelegate<T>(
                    typeof(T).GetMembersToFormat(includeInternals).ToArray()));
        }

        public static PlainTextFormatter<T> CreateForMembers(params Expression<Func<T, object>>[] members)
        {
            var format = PlainTextFormatter.CreateFormatDelegate<T>(
                typeof(T).GetMembers(members).ToArray());

            return new PlainTextFormatter<T>(format);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Part of Pattern")]
        public static PlainTextFormatter<T> CreateForAnyEnumerable(bool _includeInternals)
        {
            return new PlainTextFormatter<T>((T value, TextWriter writer, FormatContext context) =>
            {
                if (value is string)
                {
                    writer.Write(value);
                    return true;
                }

                switch (value)
                {
                    case IEnumerable enumerable:
                        Formatter.Join(enumerable,
                                       writer,
                                       context, Formatter<T>.ListExpansionLimit);
                        break;
                    default:
                        writer.Write(value.ToString());
                        break;
                }
                return true;
            });
        }

        public static PlainTextFormatter<T> Default = CreateForAnyEnumerable(false);
    }
}