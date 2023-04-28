// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.HttpRequest;

public class HttpRequestKernelExtension
{
    public static void Load(Kernel kernel)
    {
        if (kernel.RootKernel is CompositeKernel compositeKernel)
        {
            var httpRequestKernel = new HttpRequestKernel();
            compositeKernel.Add(httpRequestKernel);
            httpRequestKernel.UseValueSharing();

            RegisterFormatters();

            KernelInvocationContext.Current?.DisplayAs($"""
                Added kernel `{httpRequestKernel.Name}`. Send HTTP requests using the following syntax:

                ```
                GET https://example.com
                ```
                """, "text/markdown");
        }
    }

    private static void RegisterFormatters()
    {
        // TODO: Add Json.
        Formatter.SetPreferredMimeTypesFor(typeof(HttpResponse), HtmlFormatter.MimeType, PlainTextFormatter.MimeType);

        Formatter.Register<HttpResponse>(
            (value, context) =>
            {
                value.FormatAsHtml(context);
                return true;
            },
            HtmlFormatter.MimeType);

        Formatter.Register<HttpResponse>(
            (value, context) =>
            {
                value.FormatAsPlainText(context);
                return true;
            },
            PlainTextFormatter.MimeType);
    }
}
