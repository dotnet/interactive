// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Formatting.Http;

internal static class HttpResponseMessageFormattingExtensions
{
    internal static async Task FormatAsHtml(
        this HttpResponseMessage responseMessage,
        FormatContext context)
    {
        var response = await responseMessage.ToHttpResponseAsync();
        response.FormatAsHtml(context);
    }

    internal static async Task FormatAsPlainText(
        this HttpResponseMessage responseMessage,
        FormatContext context)
    {
        var response = await responseMessage.ToHttpResponseAsync();
        response.FormatAsPlainText(context);
    }
}