// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.Journey.Tests.Utilities;

public class FakeHttpMessageHandlerForNotebookLoading : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var notebookName = request?.RequestUri?.AbsolutePath;

        if (notebookName is null)
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest);
        }

        notebookName = notebookName.Remove(0, 1);

        var filePath = PathUtilities.GetNotebookPath(notebookName);
        if (!File.Exists(filePath))
        {
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        }

        var rawData = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(rawData)
        };

        return response;
    }
}