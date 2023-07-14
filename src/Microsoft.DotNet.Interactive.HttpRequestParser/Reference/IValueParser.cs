// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.HttpRequest.Reference;

internal interface IValueParser
{
    string ParseValues(string text, IReadOnlyDictionary<string, ParsedVariable> variablesExpanded);

    /// <summary>
    /// Get a value provider for the given type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="provider"></param>
    /// <returns></returns>
    bool TryGetValueProvider<T>([NotNullWhen(true)] out T provider) where T : class, IValueProvider;

    /// <summary>
    /// Gets a value provider with a matching prefix
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="prefix"></param>
    /// <param name="provider"></param>
    /// <returns></returns>
    bool TryGetValueProviderWithMatchingPrefix<T>(string prefix, [NotNullWhen(true)] out T provider) where T : class, IValueProvider;

    /// <summary>
    /// The rest document associated with this parser.
    /// </summary>
    ITextSnapshot Snapshot { get; set; }

    /// <summary>
    /// All the imported value providers
    /// </summary>
    public IEnumerable<IValueProvider> ValueProviders { get; }
}
