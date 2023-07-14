// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.HttpRequest.Reference;

internal interface IHttpDocumentSnapshot
{
    /// <summary>
    /// The current text snapshot
    /// </summary>
    ITextSnapshot TextSnapshot { get; }

    /// <summary>
    /// List of errors encountered while parsing
    /// </summary>
    IReadOnlyDictionary<ParseItem, IReadOnlyList<Error>> Errors { get; }

    /// <summary>
    /// List of all the parse items
    /// </summary>
    IReadOnlyList<ParseItem> Items { get; }

    /// <summary>
    /// All the requests in the document
    /// </summary>
    IReadOnlyList<Request> Requests { get; }

    /// <summary>
    /// All the evaluared variables in the document
    /// </summary>
    IReadOnlyDictionary<string, ParsedVariable> Variables { get; }

    /// <summary>
    /// The value parser for this document
    /// </summary>
    IValueParser ValueParser { get; }

    /// <summary>
    /// Finds the parse item at the given position
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    ParseItem FindItemFromPosition(int position);

    /// <summary>
    /// Gets the errors for the given parse item
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    IReadOnlyList<Error> GetErrorsForParseItem(ParseItem item);
}
