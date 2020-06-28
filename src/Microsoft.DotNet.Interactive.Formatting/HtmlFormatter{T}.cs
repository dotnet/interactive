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

        public static ITypeFormatter<T> Create(bool includeInternals = false)
        {
            if (HtmlFormatter.DefaultFormatters.TryGetFormatterForType(typeof(T), out var formatter) &&
                formatter is ITypeFormatter<T> ft)
            {
                return ft;
            }

            if (typeof(T).IsEnum)
            {
                return new HtmlFormatter<T>((enumValue, writer) => { writer.Write(enumValue.ToString()); });
            }

            if (typeof(IEnumerable).IsAssignableFrom(typeof(T)))
            {
                return CreateForSequence(includeInternals);
            }

            return CreateForObject(includeInternals);
        }

        private static HtmlFormatter<T> CreateForObject(bool includeInternals)
        {
            var members = typeof(T).GetAllMembers(includeInternals)
                                   .GetMemberAccessors<T>();

            if (members.Length == 0)
            {
                return new HtmlFormatter<T>((value, writer) => writer.Write(value));
            }

            return new HtmlFormatter<T>((instance, writer) =>
            {
                IEnumerable<object> headers = members.Select(m => m.Member.Name)
                                                     .Select(v => th(v));

                IEnumerable<object> values = members.Select(m => Value(m, instance))
                                                    .Select(v => td(v));

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

        private static HtmlFormatter<T> CreateForSequence(bool includeInternals)
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

            return new HtmlFormatter<T>((value, writer) =>
            {
                var (rowData, remainingCount) = getValues(value)
                                                .Cast<object>()
                                                .Select((v, i) => (v, i))
                                                .TakeAndCountRemaining(Formatter.ListExpansionLimit);

                if (rowData.Count == 0)
                {
                    writer.Write(i("(empty)"));
                    return;
                }

                var valuesByHeader = new Dictionary<string, Dictionary<int, object>>();

                foreach (var (v, i) in rowData)
                {
                    var destructurer = Destructurer.GetOrCreate(v?.GetType());

                    var destructured = destructurer.Destructure(v);

                    foreach (var pair in destructured)
                    {
                        valuesByHeader.GetOrAdd(pair.Key, key => new Dictionary<int, object>())
                                      .Add(i, pair.Value);
                    }
                }

                var headers = new List<IHtmlContent>();
                var rows = new List<IHtmlContent>();

                List<string> leftColumnValues;

                if (getKeys != null)
                {
                    headers.Add(th(i("key")));
                    leftColumnValues = getKeys(value).Cast<string>()
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

                headers.AddRange(valuesByHeader.Keys.Select(k => (IHtmlContent) th(k)));

                for (var rowIndex = 0; rowIndex < rowData.Count; rowIndex++)
                {
                    var rowValues = new List<object>
                    {
                        leftColumnValues[rowIndex]
                    };

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

                    rows.Add(
                        tr(
                            rowValues.Select(
                                r => td(r))));
                }

                if (remainingCount > 0)
                {
                    var more = $"({remainingCount} more)";

                    rows.Add(tr(td[colspan: $"{headers.Count}"](more)));
                }

                var table = HtmlFormatter.Table(headers, rows);

                writer.Write(table);
            });
        }

        private static string Value(MemberAccessor<T> m, T instance)
        {
            try
            {
                var value = m.GetValue(instance);
                return value.ToDisplayString();
            }
            catch (Exception exception)
            {
                return exception.ToDisplayString(PlainTextFormatter.MimeType);
            }
        }
    }
}