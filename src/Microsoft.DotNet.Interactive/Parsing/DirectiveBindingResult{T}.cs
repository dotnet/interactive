// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.Parsing;

[DebuggerStepThrough]
internal sealed class DirectiveBindingResult<T>
{
    private DirectiveBindingResult()
    {
    }

    public List<CodeAnalysis.Diagnostic> Diagnostics { get; } = new();

    public bool IsSuccessful { get; private set; }

    public T? Value { get; set; }

    public static DirectiveBindingResult<T> Success(T value, params CodeAnalysis.Diagnostic[] diagnostics)
    {
        if (diagnostics is not null &&
            diagnostics.Any(d => d.Severity is DiagnosticSeverity.Error))
        {
            throw new ArgumentException("Errors must not be present when binding is successful.", nameof(diagnostics));
        }

        var result = new DirectiveBindingResult<T>
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

    public static DirectiveBindingResult<T> Failure(params CodeAnalysis.Diagnostic[] diagnostics)
    {
        if (diagnostics is null)
        {
            throw new ArgumentNullException(nameof(diagnostics));
        }

        if (diagnostics.Length is 0)
        {
            throw new ArgumentException("Value cannot be an empty collection.", nameof(diagnostics));
        }

        if (!diagnostics.Any(e => e.Severity is DiagnosticSeverity.Error))
        {
            throw new ArgumentException("At least one error must be present when binding is unsuccessful.", nameof(diagnostics));
        }

        var result = new DirectiveBindingResult<T>
        {
            IsSuccessful = false
        };

        result.Diagnostics.AddRange(diagnostics);

        return result;
    }
}