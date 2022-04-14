// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    internal static class HtmlExtensions
    {
        public static IHtmlContent HtmlEncode(this string content)
        {
            return new HtmlString(HttpUtility.HtmlEncode(content));
        }

        public static IHtmlContent HtmlAttributeEncode(this string content)
        {
            return new HtmlString(HttpUtility.HtmlAttributeEncode(content));
        }

        public static IHtmlContent ToHtmlContent(this string value)
        {
            return new HtmlString(value);
        }
    }
}