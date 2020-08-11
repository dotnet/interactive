// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class PlainTextFormatter
    {
        public static ITypeFormatter GetPreferredFormatterFor(Type type) =>
            Formatter.GetPreferredFormatterFor(type, MimeType);

        public static ITypeFormatter GetPreferredFormatterFor<T>() =>
            GetPreferredFormatterFor(typeof(T));

        public const string MimeType = "text/plain";

        internal static ITypeFormatter GetDefaultFormatterForAnyObject(Type type, bool includeInternals = false) =>
            FormattersForAnyObject.GetFormatter(type, includeInternals);

        internal static ITypeFormatter GetDefaultFormatterForAnyEnumerable(Type type) =>
            FormattersForAnyEnumerable.GetFormatter(type, false);

        internal static Func<IFormatContext, T, TextWriter, bool> CreateFormatDelegate<T>(MemberInfo[] forMembers)
        {
            var accessors = forMembers.GetMemberAccessors<T>();

            if (Formatter<T>.TypeIsValueTuple || 
                Formatter<T>.TypeIsTuple)
            {
                return FormatValueTuple;
            }

            if (Formatter<T>.TypeIsException)
            {
                // filter out internal values from the Data dictionary, since they're intended to be surfaced in other ways
                var dataAccessor = accessors.SingleOrDefault(a => a.Member.Name == "Data");
                if (dataAccessor != null)
                {
                    var originalGetData = dataAccessor.GetValue;
                    dataAccessor.GetValue = e => ((IDictionary) originalGetData(e))
                                                 .Cast<DictionaryEntry>()
                                                 .ToDictionary(de => de.Key, de => de.Value);
                }

                // replace the default stack trace with the full stack trace when present
                var stackTraceAccessor = accessors.SingleOrDefault(a => a.Member.Name == "StackTrace");
                if (stackTraceAccessor != null)
                {
                    stackTraceAccessor.GetValue = e =>
                    {
                        var ex = e as Exception;

                        return ex.StackTrace;
                    };
                }
            }

            if (typeof(T).IsEnum)
            {
                return (context, enumValue, writer) =>
                {
                    writer.Write(enumValue.ToString());
                    return true;
                };
            }

            return FormatObject;

            bool FormatObject(IFormatContext context, T target, TextWriter writer)
            {
                Formatter.SingleLinePlainTextFormatter.WriteStartObject(writer);

                if (!Formatter<T>.TypeIsAnonymous)
                {
                    Formatter<Type>.FormatTo(context, typeof(T), writer);
                    Formatter.SingleLinePlainTextFormatter.WriteEndHeader(writer);
                }

                for (var i = 0; i < accessors.Length; i++)
                {
                    var accessor = accessors[i];

                    if (accessor.Ignore)
                    {
                        continue;
                    }

                    object value;
                    try
                    {
                        value = accessor.GetValue(target);
                    }
                    catch (Exception exception)
                    {
                        value = exception;
                    }

                    Formatter.SingleLinePlainTextFormatter.WriteStartProperty(writer);
                    writer.Write(accessor.Member.Name);
                    Formatter.SingleLinePlainTextFormatter.WriteNameValueDelimiter(writer);
                    value.FormatTo(context, writer);
                    Formatter.SingleLinePlainTextFormatter.WriteEndProperty(writer);

                    if (i < accessors.Length - 1)
                    {
                        Formatter.SingleLinePlainTextFormatter.WritePropertyDelimiter(writer);
                    }
                }

                Formatter.SingleLinePlainTextFormatter.WriteEndObject(writer);
                return true;
            }

            bool FormatValueTuple(IFormatContext context, T target, TextWriter writer)
            {
                Formatter.SingleLinePlainTextFormatter.WriteStartTuple(writer);

                for (var i = 0; i < accessors.Length; i++)
                {
                    try
                    {
                        var accessor = accessors[i];

                        if (accessor.Ignore)
                        {
                            continue;
                        }

                        var value = accessor.GetValue(target);

                        value.FormatTo(context, writer);

                        Formatter.SingleLinePlainTextFormatter.WriteEndProperty(writer);

                        if (i < accessors.Length - 1)
                        {
                            Formatter.SingleLinePlainTextFormatter.WritePropertyDelimiter(writer);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                Formatter.SingleLinePlainTextFormatter.WriteEndTuple(writer);
                return true;
            }
        }

        internal static ITypeFormatter[] DefaultFormatters { get; } = DefaultPlainTextFormatterSet.DefaultFormatters;

        internal static FormatterTable FormattersForAnyObject =
            new FormatterTable(typeof(PlainTextFormatter<>), nameof(PlainTextFormatter<object>.CreateForAnyObject));

        internal static FormatterTable FormattersForAnyEnumerable =
            new FormatterTable(typeof(PlainTextFormatter<>), nameof(PlainTextFormatter<object>.CreateForAnyEnumerable));

    }
}