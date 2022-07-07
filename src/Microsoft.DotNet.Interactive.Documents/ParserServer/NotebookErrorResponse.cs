// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Documents.ParserServer;

public class NotebookErrorResponse : NotebookParserServerResponse
{
    public NotebookErrorResponse(string id, string errorMessage)
        : base(id)
    {
        ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
    }

    public string ErrorMessage { get; }
}