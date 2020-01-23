// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive
{
    public static class Kernel
    {
        public static Func<string> DisplayIdGenerator { get; set; }

        public static DisplayedValue display(
            object value,
            string mimeType = null)
        {
            return Task.Run(() => KernelInvocationContext.Current.DisplayAsync(value, mimeType)).Result;
        }

        public static IHtmlContent HTML(string content) => content.ToHtmlContent();

        public static void Javascript(
            string scriptContent)
        {
            PocketView value =
                script[type: "text/javascript"](
                    HTML(
                        scriptContent));

            var formatted = new FormattedValue(
                HtmlFormatter.MimeType,
                value.ToString());

            var kernel = KernelInvocationContext.Current.HandlingKernel;

            Task.Run(() =>
                         kernel.SendAsync(new DisplayValue(value, formatted)))
                .Wait();
        }
    }
}