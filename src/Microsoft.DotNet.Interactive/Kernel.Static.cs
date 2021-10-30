﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive
{
    public partial class Kernel
    {
        public static Kernel Current => KernelInvocationContext.Current.HandlingKernel;

        public static Kernel Root => KernelInvocationContext.Current.HandlingKernel?.RootKernel;

        public static DisplayedValue display(
            object value,
            params string[] mimeTypes)
        {
            return value.Display(mimeTypes);
        }

        public static IHtmlContent HTML(string content) => content.ToHtmlContent();

        public static void CSS(string content) =>
            // From https://stackoverflow.com/questions/524696/how-to-create-a-style-tag-with-javascript
            Javascript($@"
            var css = '{content}',
            head = document.head || document.getElementsByTagName('head')[0],
            style = document.createElement('style');

            head.appendChild(style);

            style.type = 'text/css';
            if (style.styleSheet){{
              // This is required for IE8 and below.
              style.styleSheet.cssText = css;
            }} else {{
              style.appendChild(document.createTextNode(css));
            }}");

        public static void Javascript(string scriptContent)
        {
            PocketView value =
                script[type: "text/javascript"](
                    HTML(
                        scriptContent));

            var formatted = new FormattedValue(
                HtmlFormatter.MimeType,
                value.ToString());

            var context = KernelInvocationContext.Current;

            var kernel = context.HandlingKernel;

            Task.Run(async () =>
            {
                await kernel.SendAsync(new DisplayValue(formatted));
            }).Wait(context.CancellationToken);
        }
    }
}