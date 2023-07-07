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

    internal static HtmlFormatter<T> CreateTableFormatterForAnyEnumerable()
    {
        Func<T, IEnumerable> getKeys = null;
        Func<T, IEnumerable> getValues = instance => (IEnumerable)instance;
        bool summarize = false;

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
                summarize = true;
            }
        }

        return new HtmlFormatter<T>((value, context) =>
                                        BuildTable(value, context, getKeys, getValues, summarize));

        static bool BuildTable(
            T source,
            FormatContext context,
            Func<T, IEnumerable> getKeys,
            Func<T, IEnumerable> getValues,
            bool summarize)
        {
            context.RequireDefaultStyles();

            using var _ = context.IncrementTableDepth();
            
            if (summarize)
            {
                HtmlFormatter.FormatAndStyleAsPlainText(source, context, PlainTextSummaryFormatter.MimeType);
                return true;
            }

            var canCountRemainder = source is ICollection;

            var (rowData, remainingCount) = getValues(source)
                                            .Cast<object>()
                                            .Select((value, index) => (value, index))
                                            .TakeAndCountRemaining(Formatter.ListExpansionLimit, canCountRemainder);

            if (rowData.Count == 0)
            {
                context.Writer.Write(i("(empty)"));
                return true;
            }

            var valuesByHeader = new Dictionary<string, Dictionary<int, object>>();
            var typesAreDifferent = false;
            var types = new Dictionary<Type, int>();

            foreach (var (value, index) in rowData)
            {
                if (value is not null)
                {
                    var type = value.GetType();
                    if (!types.ContainsKey(type))
                    {
                        types.Add(type, types.Count);
                    }

                    typesAreDifferent = types.Count > 1;
                }

                valuesByHeader
                    .GetOrAdd("value", _ => new Dictionary<int, object>())
                    .Add(index, value);
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

            var valueKeys = valuesByHeader.Keys.ToArray();

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
                    var type = rowData[rowIndex].value?.GetType();

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

                rows.Add(tr(rowValues.Select(value =>
                {
                    if (value is string stringValue)
                    {
                        return td(HtmlFormatter.TagWithPlainTextStyling(stringValue, PlainTextFormatter.MimeType));
                    }
                    else
                    {
                        return td(value);
                    }
                })));
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
            return new HtmlFormatter<T>((value, context) => HtmlFormatter.FormatAndStyleAsPlainText(value, context));
        }

        (HtmlTag propertyLabelTdTag, Func<T, HtmlTag> getPropertyValueTdTag)[] rows =
            typeof(T).GetMembersToFormat()
                     .GetMemberAccessors<T>()
                     .Select(a => (
                                      new HtmlTag("td", a.MemberName),
                                      new Func<T, HtmlTag>(obj => new HtmlTag("td", c =>
                                      {
                                          var value = a.GetValueOrException(obj);
                                          value.FormatTo(c, HtmlFormatter.MimeType);
                                      }))))
                     .ToArray();

        // represent IEnumerable as a separate special property "(values)" at the end of the list
        if (typeof(T).IsEnumerable())
        {
            if (typeof(T).ShouldIncludePropertiesInOutput())
            {
                var enumerableFormatter = HtmlFormatter.GetDefaultFormatterForAnyEnumerable(typeof(T));

                (HtmlTag propertyLabelTdTag, Func<T, HtmlTag> getPropertyValueTdTag) enumerableAccessor =
                    (new HtmlTag("td", new HtmlTag("i", "(values)")), obj => new HtmlTag("td", c =>
                        {
                            enumerableFormatter.Format(obj, c);
                        }));
                Array.Resize(ref rows, rows.Length + 1);
                rows[^1] = enumerableAccessor;
            }
            else
            {
                return (HtmlFormatter<T>)HtmlFormatter.GetDefaultFormatterForAnyEnumerable(typeof(T));
            }
        }

        return new HtmlFormatter<T>((instance, context) => BuildTreeView(instance, context, rows));

        static bool BuildTreeView(T source, FormatContext context, (HtmlTag propertyLabelTdTag, Func<T, HtmlTag> getPropertyValueTdTag)[] rows)
        {
            context.RequireDefaultStyles();

            if (!context.AllowRecursion)
            {
                HtmlFormatter.FormatAndStyleAsPlainText(source, context);
                return true;
            }

            HtmlTag summaryContent = new("code", context =>
            {
                var summary = source.ToDisplayString(PlainTextSummaryFormatter.MimeType).HtmlEncode();

                context.Writer.Write(summary);
            });

            var attributes = new HtmlAttributes();

            if (context.Depth < 2)
            {
                attributes.Add("open", "open");
            }

            attributes.AddCssClass("dni-treeview");

            PocketView view = details[attributes](
                summary(
                    span[@class: "dni-code-hint"](summaryContent)),
                div(
                    Html.Table(
                        headers: null,
                        rows: rows.Select(
                            a => (IHtmlContent)
                                tr(
                                    a.propertyLabelTdTag, a.getPropertyValueTdTag(source)
                                )
                        ).ToArray())));

            view.WriteTo(context);

            return true;
        }
    }
}