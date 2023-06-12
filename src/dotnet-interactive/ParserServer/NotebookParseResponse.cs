// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Documents;

namespace Microsoft.DotNet.Interactive.App.ParserServer;

public sealed class NotebookParseResponse : NotebookParserServerResponse
{
    public InteractiveDocument Document { get; }

    public NotebookParseResponse(string id, InteractiveDocument document)
        : base(id)
    {
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }
}