// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Documents;

namespace Microsoft.DotNet.Interactive.App.ParserServer;

public sealed class NotebookSerializeRequest : NotebookParseOrSerializeRequest
{
    public override RequestType Type => RequestType.Serialize;
    public string NewLine { get; }
    public InteractiveDocument Document { get; }

    public NotebookSerializeRequest(string id, DocumentSerializationType serializationType, string defaultLanguage, string newLine, InteractiveDocument document)
        : base(id, serializationType, defaultLanguage)
    {
        NewLine = newLine ?? throw new ArgumentNullException(nameof(newLine));
        Document = document ?? throw new ArgumentNullException(nameof(document));
    }
}