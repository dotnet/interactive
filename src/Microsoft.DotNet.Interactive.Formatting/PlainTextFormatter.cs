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
        static PlainTextFormatter()
        {
            Formatter.Clearing += (sender, args) => DefaultFormatters = new DefaultPlainTextFormatterSet();
        }

        public static ITypeFormatter Create(
            Type type,
            bool includeInternals = false)
        {
            var genericCreateForAllMembers = typeof(PlainTextFormatter<>)
                                             .MakeGenericType(type)
                                             .GetMethod(nameof(PlainTextFormatter<object>.Create),
                                                        new[]
                                                        {
                                                            typeof(bool)
                                                        });

            return (ITypeFormatter) genericCreateForAllMembers.Invoke(null, new object[] { includeInternals });
        }

        public const string MimeType = "text/plain";

        internal static Action<T, TextWriter> CreateFormatDelegate<T>(MemberInfo[] forMembers)
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
                return (enumValue, writer) =>
                {
                    writer.Write(enumValue.ToString());
                };
            }

            return FormatObject;

            void FormatObject(T target, TextWriter writer)
            {
                Formatter.SingleLinePlainTextFormatter.WriteStartObject(writer);

                if (!Formatter<T>.TypeIsAnonymous)
                {
                    Formatter<Type>.FormatTo(typeof(T), writer);
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
                    value.FormatTo(writer);
                    Formatter.SingleLinePlainTextFormatter.WriteEndProperty(writer);

                    if (i < accessors.Length - 1)
                    {
                        Formatter.SingleLinePlainTextFormatter.WritePropertyDelimiter(writer);
                    }
                }

                Formatter.SingleLinePlainTextFormatter.WriteEndObject(writer);
            }

            void FormatValueTuple(T target, TextWriter writer)
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

                        value.FormatTo(writer);

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
            }
        }

        internal static IFormatterSet DefaultFormatters { get; private set; } = new DefaultPlainTextFormatterSet();
    }
}