// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class HtmlFormatter<T> : TypeFormatter<T>
    {
        private readonly FormatDelegate<T> _format;

        public HtmlFormatter(FormatDelegate<T> format)
        {
            _format = format;
        }

        public HtmlFormatter(Action<T, FormatContext> format)
        {
            _format = FormatInstance;

            bool FormatInstance(T instance, FormatContext context)
            {
                format(instance, context);
                return true;
            }
        }

        public HtmlFormatter(Func<T, string> format)
        {
            _format = (instance, context) =>
            {
                context.Writer.Write(format(instance));
                return true;
            };
        }

        public override bool Format(
            T value,
            FormatContext context)
        {
            if (value is null)
            {
                HtmlFormatter.FormatAndStyleAsPlainText(Formatter.NullString, context);
                return true;
            }

            return _format(value, context);
        }

        public override string MimeType => HtmlFormatter.MimeType;

        internal static HtmlFormatter<T> CreateForAnyObject(bool includeInternals)
        {
            var members = typeof(T).GetMembersToFormat(includeInternals)
                                   .GetMemberAccessors<T>();

            return new HtmlFormatter<T>((instance, context) =>
            {
                // Note the order of members is declaration order
                var reducedMembers = 
                    members
                        .Take(Math.Max(0, HtmlFormatter.MaxProperties))
                        .ToArray();

                if (reducedMembers.Length == 0)
                {
                    // This formatter refuses to format objects without members, and 
                    // refused to produce nested tables, or if no members are selected
                    return false;
                }
                else
                {
                    // Note, embeds the keys and values as arbitrary objects into the HTML content,
                    List<IHtmlContent> headers = 
                        reducedMembers.Select(m => (IHtmlContent)th(m.Member.Name))
                                      .ToList();
                    
                    // Add a '..' column if we elided some members due to size limitations
                    if (reducedMembers.Length < members.Length)
                    {
                        headers.Add(th(".."));
                    }

                    IEnumerable<object> values =
                        reducedMembers.Select(m => m.GetValueOrException(instance))
                                      .Select(v => td(
                                                  div[@class: "dni-plaintext"](pre(v.ToDisplayString(PlainTextFormatter.MimeType)))));

                    PocketView t =
                        table(
                            thead(
                                tr(
                                    headers)),
                            tbody(
                                tr(
                                    values)));

                    t.WriteTo(context);

                    return true;
                }
            });
        }

        internal static HtmlFormatter<T> CreateForAnyEnumerable(bool includeInternals)
        {
            Func<T, IEnumerable> getKeys = null;
            Func<T, IEnumerable> getValues = instance => (IEnumerable) instance;

            var dictType =
                typeof(T).GetAllInterfaces()
                         .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                ??
                typeof(T).GetAllInterfaces()
                         .FirstOrDefault(i => i == typeof(IDictionary));

            if (dictType is not null)
            {
                var keysProperty = dictType.GetProperty("Keys");
                getKeys = instance => (IEnumerable) keysProperty.GetValue(instance, null);

                var valuesProperty = dictType.GetProperty("Values");
                getValues = instance => (IEnumerable) valuesProperty.GetValue(instance, null);
            }

            return new HtmlFormatter<T>(BuildTable);

            bool BuildTable(T source, FormatContext context)
            {
                using var _ = context.IncrementTableDepth();

                if (context.TableDepth > 1)
                {
                    HtmlFormatter.FormatAndStyleAsPlainText(source,  context);
                    return true;
                }

                var canCountRemainder = source is ICollection;

                var (rowData, remainingCount) = getValues(source)
                                                .Cast<object>()
                                                .Select((v, i) => (v, i))
                                                .TakeAndCountRemaining(Formatter.ListExpansionLimit, canCountRemainder);

                if (rowData.Count == 0)
                {
                    context.Writer.Write(i("(empty)"));
                    return true;
                }

                var valuesByHeader = new Dictionary<string, Dictionary<int, object>>();
                var headerToSortIndex = new Dictionary<string, (int, int)>();
                var typesAreDifferent = false;
                var types = new Dictionary<Type, int>();

                foreach (var (value, index) in rowData)
                {
                    IDictionary<string, object> keysAndValues;

                    if (value is { } &&
                        Formatter.GetPreferredFormatterFor(value.GetType(), HtmlFormatter.MimeType) is { } formatter &&
                        formatter.Type == typeof(object))
                    {
                        var destructurer = Destructurer.GetOrCreate(value?.GetType());

                        keysAndValues = destructurer.Destructure(value);
                    }
                    else
                    {
                        keysAndValues = NonDestructurer.Instance.Destructure(value);
                    }

                    if (value is not null)
                    {
                        var type = value.GetType();
                        if (!types.ContainsKey(type))
                        {
                            types.Add(type, types.Count);
                        }

                        typesAreDifferent = types.Count > 1;
                    }

                    var typeIndex = value is null ? 0 : types[value.GetType()];

                    var pairIndex = 0;

                    foreach (var pair in keysAndValues)
                    {
                        if (!headerToSortIndex.ContainsKey(pair.Key))
                        {
                            headerToSortIndex.Add(pair.Key, (typeIndex, pairIndex));
                        }

                        valuesByHeader
                            .GetOrAdd(pair.Key, _ => new Dictionary<int, object>())
                            .Add(index, pair.Value);
                        pairIndex++;
                    }
                }

                var headers = new List<IHtmlContent>();

                List<object> leftColumnValues;

                if (getKeys is not null)
                {
                    headers.Add(th(i("key")));
                    leftColumnValues = getKeys(source)
                                       .Cast<object>()
                                       .Take(rowData.Count)
                                       .ToList();
                }
                else
                {
                    headers.Add(th(i("index")));
                    leftColumnValues = Enumerable.Range(0, rowData.Count)
                                                 .Select(i => (object) new HtmlString(i.ToString()))
                                                 .ToList();
                }

                if (typesAreDifferent)
                {
                    headers.Insert(1, th(i("type")));
                }

                // Order the columns first by the *first* type to exhibit the
                // property, then by the destructuring order within that type.
                var valueKeys =
                    valuesByHeader.Keys
                       .OrderBy(x => headerToSortIndex[x])
                       .ToArray();

                var valueKeysLimited =
                    valueKeys
                        .Take(Math.Max(0, HtmlFormatter.MaxProperties))
                        .ToArray();

                headers.AddRange(valueKeysLimited.Select(k => (IHtmlContent) th(k)));
                if (valueKeysLimited.Length < valueKeys.Length)
                {
                    headers.Add((IHtmlContent)th(".."));
                }
                
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

                    foreach (var key in valueKeysLimited)
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

                    rows.Add(tr(rowValues.Select(r => td(r))));
                }

                if (remainingCount > 0)
                {
                    rows.Add(tr(td[colspan: $"{headers.Count}"](i($"({remainingCount} more)"))));
                }
                else if (remainingCount is null)
                {
                    rows.Add(tr(td[colspan: $"{headers.Count}"](i("... (more)"))));
                }

                var table = Html.Table(headers, rows);

                table.WriteTo(context);
                return true;
            }
        }
    }
}