// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

using Diagnostic = CodeAnalysis.Diagnostic;

internal class HttpBindingResult<T>
{
    private HttpBindingResult()
    {
    }

    public List<Diagnostic> Diagnostics { get; } = new();
    public bool IsSuccessful { get; private set; }

    public T? Value { get; set; }

    public static HttpBindingResult<T> Success(T value) => new()
    {
        IsSuccessful = true,
        Value = value
    };

    public static HttpBindingResult<T> Failure(params Diagnostic[] diagnostics)
    {
        if (diagnostics is null)
        {
            throw new ArgumentNullException(nameof(diagnostics));
        }

        if (diagnostics.Length == 0)
        {
            throw new ArgumentException("Value cannot be an empty collection.", nameof(diagnostics));
        }

        var result = new HttpBindingResult<T>
        {
            IsSuccessful = false
        };

        result.Diagnostics.AddRange(diagnostics);

        return result;
    }
}