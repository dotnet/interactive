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
            Formatter.Clearing += (obj, sender) =>
            {
                MaxProperties = DefaultMaxProperties;
            };
        }

        public static ITypeFormatter GetPreferredFormatterFor(Type type) =>
            Formatter.GetPreferredFormatterFor(type, MimeType);

        public static ITypeFormatter GetPreferredFormatterFor<T>() =>
            GetPreferredFormatterFor(typeof(T));

        public const string MimeType = "text/plain";

        /// <summary>
        ///   Indicates the maximum number of properties to show in the default plaintext display of arbitrary objects.
        ///   If set to zero no properties are shown.
        /// </summary>
        public static int MaxProperties { get; set; } = DefaultMaxProperties;

        internal const int DefaultMaxProperties = 20;

        internal static ITypeFormatter GetDefaultFormatterForAnyObject(Type type, bool includeInternals = false) =>
            FormattersForAnyObject.GetFormatter(type, includeInternals);

        internal static ITypeFormatter GetDefaultFormatterForAnyEnumerable(Type type) =>
            FormattersForAnyEnumerable.GetFormatter(type, false);

        internal static Func<FormatContext, T, TextWriter, bool> CreateFormatDelegate<T>(MemberInfo[] forMembers)
        {
            var accessors = forMembers.GetMemberAccessors<T>().Where(i => !i.Ignore).ToArray();

            if (Formatter<T>.TypeIsValueTuple || 
                Formatter<T>.TypeIsTuple)
            {
                return FormatAnyTuple;
            }

            if (Formatter<T>.TypeIsException)
            {
                // filter out internal values from the Data dictionary, since they're intended to be surfaced in other ways
                var dataAccessor = accessors.SingleOrDefault(a => a.Member.Name == "Data");
                if (dataAccessor != null)
                {
                    var originalGetData = dataAccessor.Getter;
                    dataAccessor.Getter = e => ((IDictionary) originalGetData(e))
                                                 .Cast<DictionaryEntry>()
                                                 .ToDictionary(de => de.Key, de => de.Value);
                }

                // replace the default stack trace with the full stack trace when present
                var stackTraceAccessor = accessors.SingleOrDefault(a => a.Member.Name == "StackTrace");
                if (stackTraceAccessor != null)
                {
                    stackTraceAccessor.Getter = e =>
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

            bool FormatObject(FormatContext context, T target, TextWriter writer)
            {

                // Greatly reduce the number of properties to show. The `ToString()` 
                // already counts as significant content.  
                var maxProperties = 
                    context.ContentThreshold <= FormatContext.NestedInTable
                    ? 0 
                    : (int)(MaxProperties * context.ContentThreshold * context.ContentThreshold);

                var reducedAccessors = accessors.Take(Math.Max(0, MaxProperties)).ToArray();

                // If we haven't got any members to show, just resort to ToString()
                if (reducedAccessors.Length == 0 || context.ContentThreshold < 0.9)
                {
                    // Write using `ToString()`
                    writer.Write(target);
                    return true;
                }

                Formatter.SingleLinePlainTextFormatter.WriteStartObject(writer);

                if (!Formatter<T>.TypeIsAnonymous)
                {
                    // Write using `ToString()`
                    writer.Write(target);
                    Formatter.SingleLinePlainTextFormatter.WriteEndHeader(writer);
                }

                for (var i = 0; i < reducedAccessors.Length; i++)
                {
                    var accessor = reducedAccessors[i];

                    object value = accessor.GetValueOrException(target);

                    Formatter.SingleLinePlainTextFormatter.WriteStartProperty(writer);
                    writer.Write(accessor.Member.Name);
                    Formatter.SingleLinePlainTextFormatter.WriteNameValueDelimiter(writer);
                    value.FormatTo(context, writer);
                    Formatter.SingleLinePlainTextFormatter.WriteEndProperty(writer);

                    if (i < reducedAccessors.Length - 1)
                    {
                        Formatter.SingleLinePlainTextFormatter.WritePropertyDelimiter(writer);
                    }
                }

                Formatter.SingleLinePlainTextFormatter.WriteEndObject(writer);
                return true;
            }

            bool FormatAnyTuple(FormatContext context, T target, TextWriter writer)
            {
                Formatter.SingleLinePlainTextFormatter.WriteStartTuple(writer);

                for (var i = 0; i < accessors.Length; i++)
                {
                    var value = accessors[i].GetValueOrException(target);

                    value.FormatTo(context, writer);

                    Formatter.SingleLinePlainTextFormatter.WriteEndProperty(writer);

                    if (i < accessors.Length - 1)
                    {
                        Formatter.SingleLinePlainTextFormatter.WritePropertyDelimiter(writer);
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