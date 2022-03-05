// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Text;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;

namespace Microsoft.DotNet.Interactive.CSharpProject.Models.Execution
{
    public static class HttpRequestExtensions
    {
        public static HttpRequestMessage ToHttpRequestMessage(this HttpRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            return new HttpRequestMessage(
                new HttpMethod(request.Verb),
                request.Uri)
            {
                Content = new StringContent(request.Body, Encoding.UTF8, "application/json")
            };
        }
    }
}
