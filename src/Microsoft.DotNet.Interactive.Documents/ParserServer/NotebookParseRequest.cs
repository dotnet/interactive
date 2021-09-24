﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Documents.ParserServer
{
    public class NotebookParseRequest : NotebookParseOrSerializeRequest
    {
        public override RequestType Type => RequestType.Parse;
        public byte[] RawData { get; }

        public NotebookParseRequest(string id, DocumentSerializationType serializationType, string defaultLanguage, byte[] rawData)
            : base(id, serializationType, defaultLanguage)
        {
            RawData = rawData ?? throw new ArgumentNullException(nameof(rawData));
        }
    }
}
