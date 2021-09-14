// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Documents.ParserServer
{
    public class NotebookParseResponse : NotebookParserServerResponse
    {
        public InteractiveDocument Document { get; }

        public NotebookParseResponse(string id, InteractiveDocument document)
            : base(id)
        {
            Document = document ?? throw new ArgumentNullException(nameof(document));
        }
    }
}
