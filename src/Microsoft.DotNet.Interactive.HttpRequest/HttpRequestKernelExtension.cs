// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;

namespace Microsoft.DotNet.Interactive.HttpRequest;

public class HttpRequestKernelExtension
{
    public static void Load(
        Kernel kernel,
        HttpClient? httpClient = null,
        int responseDelayThresholdInMilliseconds = HttpRequestKernel.DefaultResponseDelayThresholdInMilliseconds,
        int contentByteLengthThreshold = HttpRequestKernel.DefaultContentByteLengthThreshold)
    {
        if (kernel.RootKernel is CompositeKernel compositeKernel)
        {
            var httpRequestKernel =
                new HttpRequestKernel(
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
