// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.App.ParserServer;

public sealed class NotebookSerializeResponse : NotebookParserServerResponse
{
    public byte[] RawData { get; }

    public NotebookSerializeResponse(string id, byte[] rawData)
        : base(id)
    {
        RawData = rawData ?? throw new ArgumentNullException(nameof(rawData));
    }
}