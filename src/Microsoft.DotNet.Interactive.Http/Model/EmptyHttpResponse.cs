// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Http;

[TypeFormatterSource(
    typeof(HttpResponseFormatterSource),
    PreferredMimeTypes = new[] { HtmlFormatter.MimeType })]
public class EmptyHttpResponse
{
}
