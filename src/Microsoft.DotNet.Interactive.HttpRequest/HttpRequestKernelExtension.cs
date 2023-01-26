﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


namespace Microsoft.DotNet.Interactive.HttpRequest;

public class HttpRequestKernelExtension 
{
    public static void  Load(Kernel kernel)
    {
        if (kernel.RootKernel is CompositeKernel compositeKernel)
        {
            var httpRequestKernel = new HttpRequestKernel();
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
