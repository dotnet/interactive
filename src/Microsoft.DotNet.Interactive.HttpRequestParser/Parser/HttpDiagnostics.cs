// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal static class HttpDiagnostics
{
    internal static HttpDiagnosticInfo GetDiagnosticInfo(
        this WellKnownHttpDiagnostics diagnosticCode,
        params object[] messageArguments)
    {
        var id = $"HTTP{(int)diagnosticCode:0000}";
        string messageFormat;
        var severity = DiagnosticSeverity.Error;

        switch (diagnosticCode)
        {
            case WellKnownHttpDiagnostics.UnrecognizedVerb:
                messageFormat = "Unrecognized HTTP verb '{0}'.";
                break;

            case WellKnownHttpDiagnostics.MissingUrl:
                messageFormat = "Missing URL.";
                break;

            case WellKnownHttpDiagnostics.UnrecognizedUriScheme:
                messageFormat = "Unrecognized URI scheme: '{0}'.";
                severity = DiagnosticSeverity.Warning;
                break;

            case WellKnownHttpDiagnostics.InvalidUri:
                messageFormat = "Invalid URI.";
                break;

            case WellKnownHttpDiagnostics.InvalidHttpVersion:
                messageFormat = "Invalid HTTP version.";
                break;

            case WellKnownHttpDiagnostics.InvalidWhitespaceInHeaderName:
                messageFormat = "Invalid whitespace in header name.";
                break;

            case WellKnownHttpDiagnostics.MissingHeaderName:
                messageFormat = "Missing header name.";
                break;

            case WellKnownHttpDiagnostics.MissingHeaderValue:
                messageFormat = "Missing header value.";
                break;

            case WellKnownHttpDiagnostics.InvalidHeaderValue:
                messageFormat = "Invalid header value: {0}.";
                break;

            case WellKnownHttpDiagnostics.VariableNameExpected:
                messageFormat = "Variable name expected.";
                break;

            case WellKnownHttpDiagnostics.CannotResolveSymbol:
                messageFormat = "Cannot resolve symbol '{0}'.";
                break;

            default:
                throw new ArgumentException($"Unrecognized diagnostic code '{diagnosticCode.ToString()}'.");
        }

        return new HttpDiagnosticInfo(id, messageFormat, severity, messageArguments);
    }
}
