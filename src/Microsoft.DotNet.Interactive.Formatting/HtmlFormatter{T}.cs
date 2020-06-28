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
                return new HtmlFormatter<T>((enumValue, writer) =>
                {
                    writer.Write(enumValue.ToString());
                });
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

            if (members.Length ==0)
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
            Func<T, IEnumerable> getValues = instance => (IEnumerable)instance;

            var dictionaryGenericType = typeof(T).GetAllInterfaces()
                                                 .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
            var dictionaryObjectType = typeof(T).GetAllInterfaces()
                                                .FirstOrDefault(i => i == typeof(IDictionary));

            if (dictionaryGenericType != null || dictionaryObjectType != null)
            {
                var keysProperty = typeof(T).GetProperty("Keys");
                getKeys = instance => (IEnumerable)keysProperty.GetValue(instance, null);

                var valuesProperty = typeof(T).GetProperty("Values");
                getValues = instance => (IEnumerable)valuesProperty.GetValue(instance, null);
            }

            return new HtmlFormatter<T>((value, writer) =>
            {
                var index = 0;

                IHtmlContent indexHeader = null;

                Func<string> getIndex;
                //
                // var valuesToFormat = 
                //
                // var headers = value.OfType<>
                //
                // if (value is IDictionary)
                // {
                //     
                // }
                //

                if (getKeys != null)
                {
                    var keys = new List<string>();
                    foreach (var key in getKeys(value))
                    {
                        keys.Add(key.ToString());
                    }

                    getIndex = () => keys[index];
                    indexHeader = th(i("key"));
                }
                else
                {
                    getIndex = () => index.ToString();
                    indexHeader = th(i("index"));
                }

                var rows = new List<IHtmlContent>();
                List<IHtmlContent> headers = null;

                var values = getValues(value);

                foreach (var item in values)
                {
                    if (index < Formatter.ListExpansionLimit)
                    {
                        IDestructurer destructurer;

                        if (item == null)
                        {
                            destructurer = NonDestructurer.Instance;
                        }
                        else
                        {
                            destructurer = Destructurer.GetOrCreate(item.GetType());
                        }

                        var dictionary = destructurer.Destructure(item);

                        if (headers == null)
                        {
                            headers = new List<IHtmlContent>();
                            headers.Add(indexHeader);
                            headers.AddRange(dictionary.Keys
                                                       .Select(k => (IHtmlContent) th(k)));
                        }

                        var cells =
                            new IHtmlContent[]
                                {
                                    td(getIndex().ToHtmlContent())
                                }
                                .Concat(
                                    dictionary
                                        .Values
                                        .Select(v => (IHtmlContent) td(v)));

                        rows.Add(tr(cells));

                        index++;
                    }
                    else
                    {
                        var more = values switch
                        {
                            ICollection c => $"({c.Count - index} more)",
                            _ => "(more...)"
                        };

                        rows.Add(tr(td[colspan: $"{headers.Count}"](more)));
                        break;
                    }
                }

                if (headers == null)
                {
                    headers = new List<IHtmlContent>
                    {
                        indexHeader,
                        th("value")
                    };
                }

                var view = HtmlFormatter.Table(headers, rows);

                view.WriteTo(writer, HtmlEncoder.Default);
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