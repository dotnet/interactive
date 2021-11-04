// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public delegate bool FormatDelegate<in T>(T value, FormatContext context);

    public class PlainTextFormatter<T> : TypeFormatter<T>
    {
        private readonly FormatDelegate<T> _format;

        public PlainTextFormatter(FormatDelegate<T> format)
        {
            _format = format ?? throw new ArgumentNullException(nameof(format));
        }

        public PlainTextFormatter(Action<T, FormatContext> format)
        {
            _format = FormatInstance;

            bool FormatInstance(T instance, FormatContext context)
            {
                format(instance, context);
                return true;
            }
        }

        public PlainTextFormatter(Func<T, string> format)
        {
            _format = (instance, context) =>
            {
                context.Writer.Write(format(instance));
                return true;
            };
        }

        public override string MimeType => PlainTextFormatter.MimeType;

        public override bool Format(T value, FormatContext context)
        {
            if (value is null)
            {
                context.Writer.Write(Formatter.NullString);
                return true;
            }

            return _format(value, context);
        }

        public static PlainTextFormatter<T> CreateForAnyObject(bool includeInternals = false)
        {
            if (typeof(T).IsScalar())
            {
                return new PlainTextFormatter<T>((value, context) =>
                {
                    context.Writer.Write(value);
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
        public static PlainTextFormatter<T> CreateForAnyEnumerable(bool includeInternals = false)
        {
            return new((value, context) =>
            {
                if (value is string)
                {
                    context.Writer.Write(value);
                    return true;
                }

                switch (value)
                {
                    case IEnumerable enumerable:
                        Formatter.Join(enumerable,
                                       context.Writer,
                                       context, Formatter<T>.ListExpansionLimit);
                        break;
                    default:
                        context.Writer.Write(value.ToString());
                        break;
                }
                return true;
            });
        }

        public static PlainTextFormatter<T> Default = CreateForAnyEnumerable(false);
    }
}