// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Utility;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting;

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

    internal static HtmlFormatter<T> CreateTableFormatterForAnyObject()
    {
        var members = typeof(T).GetMembersToFormat()
                               .GetMemberAccessors<T>();

        return new HtmlFormatter<T>((instance, context) => BuildTable(instance, context, members));

        static bool BuildTable(T instance, FormatContext context, MemberAccessor<T>[] memberAccessors)
        {
            if (memberAccessors.Length == 0)
            {
                // This formatter refuses to format objects without members, and 
                // refused to produce nested tables, or if no members are selected
                return false;
            }
            else
            {
                // Note, embeds the keys and values as arbitrary objects into the HTML content,
                List<IHtmlContent> headers =
                    memberAccessors.Select(m => (IHtmlContent)th(m.Member.Name))
                                   .ToList();

                // FIX: (CreateTableFormatterForAnyObject) should this use a tree view?
                IEnumerable<object> values =
                    memberAccessors.Select(m => m.GetValueOrException(instance))
                                   .Select(v => td(div[@class: "dni-plaintext"](pre(v.ToDisplayString(PlainTextFormatter.MimeType)))));

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
        }
    }

    internal static HtmlFormatter<T> CreateTableFormatterForAnyEnumerable()
    {
        Func<T, IEnumerable> getKeys = null;
        Func<T, IEnumerable> getValues = instance => (IEnumerable)instance;
        bool flatten = false;

        if (typeof(T).IsDictionary(
                out var getKeys1,
                out var getValues1,
                out var keyType1,
                out var valueType1))
        { 
            getKeys = instance => getKeys1(instance);  
            getValues = instance => getValues1(instance);
        }
        else
        {
            var elementType = typeof(T).GetElementTypeIfEnumerable();

            if (elementType?.IsScalar() is true)
            {
                flatten = true;
            }
        }

        return new HtmlFormatter<T>((value, context) => BuildTable(value, context, flatten));

        bool BuildTable(T source, FormatContext context, bool summarize)
        {
            context.RequireDefaultStyles();

            using var _ = context.IncrementTableDepth();

            if (summarize || !context.AllowRecursion)
            {
                HtmlFormatter.FormatAndStyleAsPlainText(source, context);
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

                // FIX: (CreateTableFormatterForAnyEnumerable) 
#if false
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
#else
                keysAndValues = NonDestructurer.Instance.Destructure(value);
#endif

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
                                             .Select(i => (object)new HtmlString(i.ToString()))
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

            headers.AddRange(valueKeys.Select(k => (IHtmlContent)th(k)));
           
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

                foreach (var key in valueKeys)
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

    internal static HtmlFormatter<T> CreateTreeViewFormatterForAnyObject()
    {
        if (typeof(T).IsScalar())
        {
            return new HtmlFormatter<T>((value, context) => { HtmlFormatter.FormatAndStyleAsPlainText(value, context); });
        }

        var members = typeof(T).GetMembersToFormat()
                               .GetMemberAccessors<T>();

        return new HtmlFormatter<T>((instance, context) => BuildTreeView(instance, context, members));

        static bool BuildTreeView(T source, FormatContext context, MemberAccessor<T>[] memberAccessors)
        {
            context.RequireDefaultStyles();

            if (!context.AllowRecursion)
            {
                HtmlFormatter.FormatAndStyleAsPlainText(source, context);
                return true;
            }

            PocketView view = null;

            HtmlTag code = new HtmlTag("code", c =>
            {
                var formatter = PlainTextSummaryFormatter.GetPreferredFormatterFor(source?.GetType());

                formatter.Format(source, context);
            });

            var attributes = new HtmlAttributes();

            if (context.Depth < 2)
            {
                attributes.Add("open", "open");
            }

            attributes.AddCssClass("dni-treeview");

            view = details[attributes](
                summary(
                    span[@class: "dni-code-hint"](code)),
                div(
                    Html.Table(
                        headers: null,
                        rows: memberAccessors.Select(
                            a => (IHtmlContent)
                                tr(
                                    td(a.Member.Name), td(a.GetValueOrException(source)))).ToArray())));

            view.WriteTo(context);

            return true;
        }
    }
}