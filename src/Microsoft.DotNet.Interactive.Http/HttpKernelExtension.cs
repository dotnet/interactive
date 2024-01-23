// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;

namespace Microsoft.DotNet.Interactive.Http;

public class HttpKernelExtension
{
    public static void Load(
        Kernel kernel,
        HttpClient? httpClient = null,
        int responseDelayThresholdInMilliseconds = HttpKernel.DefaultResponseDelayThresholdInMilliseconds,
        int contentByteLengthThreshold = HttpKernel.DefaultContentByteLengthThreshold)
    {
        if (kernel.RootKernel is CompositeKernel compositeKernel)
        {
            var httpRequestKernel =
                new HttpKernel(
                    client: httpClient,
                    responseDelayThresholdInMilliseconds: responseDelayThresholdInMilliseconds,
                    contentByteLengthThreshold: contentByteLengthThreshold);

            compositeKernel.Add(httpRequestKernel);
            httpRequestKernel.UseValueSharing();

            KernelInvocationContext.Current?.DisplayAs($"""
                Added kernel `{httpRequestKernel.Name}`. Send HTTP requests using the following syntax:

                ```
                GET https://example.com
                ```
                """, "text/markdown");
        }
    }
}

internal static class DateTimeOffsetExtensions
{
    public static DateTimeOffset AddOffset(this DateTimeOffset dateTime, int offset, string options)
    {
        return options switch
        {
            "ms" => dateTime.AddMilliseconds(offset),
            "s" => dateTime.AddSeconds(offset),
            "m" => dateTime.AddMinutes(offset),
            "h" => dateTime.AddHours(offset),
            "d" => dateTime.AddDays(offset),
            "w" => dateTime.AddDays(offset * 7),
            "M" => dateTime.AddMonths(offset),
            "Q" => dateTime.AddMonths(offset * 3),
            "y" => dateTime.AddYears(offset),
            _ => throw new ArgumentException("Invalid offset option", nameof(options))
        };
    }
}
