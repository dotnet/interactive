// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

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

    public static HttpBindingResult<T> Success(T value, params Diagnostic[] diagnostics)
    {
        if (diagnostics is not null &&
            diagnostics.Any(d => d.Severity is CodeAnalysis.DiagnosticSeverity.Error))
        {
            throw new ArgumentException("Errors must not be present when binding is successful.", nameof(diagnostics));
        }

        var result = new HttpBindingResult<T>
        {
            IsSuccessful = true,
            Value = value
        };

        if (diagnostics is not null)
        {
            result.Diagnostics.AddRange(diagnostics);
        }

        return result;
    }

    public static HttpBindingResult<T> Failure(params Diagnostic[] diagnostics)
    {
        if (diagnostics is null)
        {
            throw new ArgumentNullException(nameof(diagnostics));
        }

        if (diagnostics.Length is 0)
        {
            throw new ArgumentException("Value cannot be an empty collection.", nameof(diagnostics));
        }

        if (!diagnostics.Any(e => e.Severity is CodeAnalysis.DiagnosticSeverity.Error))
        {
            throw new ArgumentException("At least one error must be present when binding is unsuccessful.", nameof(diagnostics));
        }

        var result = new HttpBindingResult<T>
        {
            IsSuccessful = false
        };

        result.Diagnostics.AddRange(diagnostics);

        return result;
    }
}