// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Web;
using Microsoft.AspNetCore.Html;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class Html
    {
        internal static IHtmlContent EnsureHtmlAttributeEncoded(this object source) =>
            source == null
                ? HtmlString.Empty
                : source as IHtmlContent ?? source.ToString().HtmlAttributeEncode();

        public static IHtmlContent HtmlEncode(this string content) =>
            new HtmlString(HttpUtility.HtmlEncode(content));

        public static IHtmlContent HtmlAttributeEncode(this string content) => new HtmlString(HttpUtility.HtmlAttributeEncode(content));

        public static IHtmlContent ToHtmlContent(this string value) =>
            new HtmlString(value);

        public static JsonString SerializeToJson<T>(this T source) =>
            new JsonString(JsonConvert.SerializeObject(source));

        public static IHtmlContent JsonEncode(this string source) =>
            new JsonString(JsonConvert.ToString(source));
    }
}