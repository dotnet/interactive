// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.App.IntegrationTests
{
    internal static class HttpClientTestExtensions
    {
        public static async Task<HttpResponseMessage> PostJsonAsync(this HttpClient client, string requestUri,
            JObject requestBody)
        {
            var content = new StringContent(requestBody.ToString(), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(requestUri, content);
            return response;
        }

        public static async Task ShouldSucceed(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                // this block is wrapped so `response.Content` isn't prematurely consumed
                var content = await response.Content.ReadAsStringAsync();
                response.IsSuccessStatusCode
                    .Should()
                    .BeTrue(
                        $"Response status code indicates failure: {(int)response.StatusCode} ({response.StatusCode}):\n{content}");
            }
        }
    }
}