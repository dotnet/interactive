// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class HtmlFormatter<T> : TypeFormatter<T>
    {
        private readonly Action<T, TextWriter> _format;

        public HtmlFormatter(Action<T, TextWriter> format)
        {
            _format = format;
        }

        public override void Format(T value, TextWriter writer)
        {
            if (value is null)
            {
                writer.Write(Formatter.NullString.HtmlEncode());
                return;
            }

            _format(value, writer);
        }

        public override string MimeType => HtmlFormatter.MimeType;

        internal static HtmlFormatter<T> CreateForAnyObject(bool includeInternals)
        {
            var members = typeof(T).GetMembersToFormat(includeInternals)
                                   .GetMemberAccessors<T>();

            if (members.Length == 0)
            {
                return new HtmlFormatter<T>((value, writer) =>
                {
                    var html = HtmlFormatter.DisplayEmbeddedObjectAsPlainText(value);

                    html.WriteTo(writer, HtmlEncoder.Default);
                });
            }

            return new HtmlFormatter<T>((instance, writer) =>
            {
                // Note, embeds the keys and values as arbitrary objects into the HTML content,
                // ultimately rendered by PocketView, e.g. via ToDisplayString(PlainTextFormatter.MimeType)
                IEnumerable<object> headers = members.Select(m => m.Member.Name)
                                                     .Select(v => th(arbitrary(v)));

                IEnumerable<object> values = members.Select(m => Value(m, instance))
                                                    .Select(v => td(arbitrary(v)));

                var t =
                    table(
                        thead(
                            tr(
                                headers)),
                        tbody(
                            tr(
                                values)));

                ((PocketView) t).WriteTo(writer, HtmlEncoder.Default);
            });
        }

        internal static HtmlFormatter<T> CreateForAnyEnumerable()
        {
            Func<T, IEnumerable> getKeys = null;
            Func<T, IEnumerable> getValues = instance => (IEnumerable) instance;

            var dictionaryGenericType = typeof(T).GetAllInterfaces()
                                                 .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            var dictionaryObjectType = typeof(T).GetAllInterfaces()
                                                .FirstOrDefault(i => i == typeof(IDictionary));

            if (dictionaryGenericType != null || dictionaryObjectType != null)
            {
                var keysProperty = typeof(T).GetProperty("Keys");
                getKeys = instance => (IEnumerable) keysProperty.GetValue(instance, null);

                var valuesProperty = typeof(T).GetProperty("Values");
                getValues = instance => (IEnumerable) valuesProperty.GetValue(instance, null);
            }

            return new HtmlFormatter<T>(BuildTable);

            void BuildTable(T source, TextWriter writer)
            {
                var (rowData, remainingCount) = getValues(source)
                                                .Cast<object>()
                                                .Select((v, i) => (v, i))
                                                .TakeAndCountRemaining(Formatter.ListExpansionLimit);

                if (rowData.Count == 0)
                {
                    writer.Write(i("(empty)"));
                    return;
                }

                var valuesByHeader = new Dictionary<string, Dictionary<int, object>>();
                bool typesAreDifferent = false;
                var types = new HashSet<Type>();

                foreach (var (value, index) in rowData)
                {
                    var destructurer = Destructurer.GetOrCreate(value?.GetType());

                    var destructured = destructurer.Destructure(value);

                    if (!typesAreDifferent && value is {})
                    {
                        types.Add(value.GetType());

                        typesAreDifferent = types.Count > 1;
                    }

                    foreach (var pair in destructured)
                    {
                        valuesByHeader
                            .GetOrAdd(pair.Key, key => new Dictionary<int, object>())
                            .Add(index, pair.Value);
                    }
                }

                var headers = new List<IHtmlContent>();

                List<string> leftColumnValues;

                if (getKeys != null)
                {
                    headers.Add(th(i("key")));
                    leftColumnValues = getKeys(source)
                                       .Cast<string>()
                                       .Take(rowData.Count)
                                       .ToList();
                }
                else
                {
                    headers.Add(th(i("index")));
                    leftColumnValues = Enumerable.Range(0, rowData.Count)
                                                 .Select(i => i.ToString())
                                                 .ToList();
                }

                if (typesAreDifferent)
                {
                    headers.Insert(1, th(i("type")));

                }

                headers.AddRange(valuesByHeader.Keys.Select(k => (IHtmlContent) th(k)));

                var rows = new List<IHtmlContent>();

                for (var rowIndex = 0; rowIndex < rowData.Count; rowIndex++)
                {
                    var rowValues = new List<object>
                    {
                        leftColumnValues[rowIndex]
                    };

                    if (typesAreDifferent)
                    {
                        var type = rowData[rowIndex].v?.GetType();

                        rowValues.Add(type);
                    }

                    foreach (var key in valuesByHeader.Keys)
                    {
                        if (valuesByHeader[key].TryGetValue(rowIndex, out var cellData))
                        {
                            rowValues.Add(cellData);
                        }
                        else
                        {
                            rowValues.Add("");
                        }
                    }

                    // Note, embeds the values as arbitrary objects into the HTML content.
                    rows.Add(tr(rowValues.Select(r => td(arbitrary(r)))));
                }

                if (remainingCount > 0)
                {
                    var more = $"({remainingCount} more)";

                    rows.Add(tr(td[colspan: $"{headers.Count}"](more)));
                }

                var table = HtmlFormatter.Table(headers, rows);

                writer.Write(table);
            }
        }

        private static object Value(MemberAccessor<T> m, T instance)
        {
            try
            {
                var value = m.GetValue(instance);
                return value;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }
}