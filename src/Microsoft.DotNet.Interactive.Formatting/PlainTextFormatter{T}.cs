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

        public PlainTextFormatter(Action<T, TextWriter> format)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format));
        }

        public override string MimeType => PlainTextFormatter.MimeType;

        public override void Format(T value, TextWriter writer)
        {
            if (value is null)
            {
                writer.Write(Formatter.NullString);
                return;
            }

            _format(value, writer);
        }

        public static PlainTextFormatter<T> CreateForAnyObject(bool includeInternals = false)
        {
            if (typeof(T).IsScalar())
            {
                return new PlainTextFormatter<T>((value, writer) => writer.Write(value));
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
            return new PlainTextFormatter<T>((T value, TextWriter writer) =>
            {
                if (value is string)
                {
                    writer.Write(value);
                    return;
                }

                switch (value)
                {
                    case IEnumerable enumerable:
                        Formatter.Join(
                            enumerable,
                            writer,
                            Formatter<T>.ListExpansionLimit);
                        break;
                    default:
                        writer.Write(value.ToString());
                        break;
                }
            });
        }

        public static PlainTextFormatter<T> Default = CreateForAnyEnumerable(false);
    }
}