// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.CSharpProject.Protocol
{
    public class HttpRequest
    {
        public HttpRequest(string uri, string verb, string body = null)
        {
            if (!Uri.TryCreate(uri, UriKind.Relative, out var parseduri))
            {
                throw new ArgumentException("Value must be a valid relative uri", nameof(uri));
            }

            if (string.IsNullOrWhiteSpace(verb))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(verb));
            }

            Uri = parseduri;
            Verb = verb;
            Body = body ?? string.Empty;
        }

        public Uri Uri { get; }

        public string Verb { get; }

        public string Body { get; }
    }
}
