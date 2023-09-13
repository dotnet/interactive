// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal static class HttpDiagnostics
{
    internal static HttpDiagnosticInfo UnrecognizedVerb(string verb)
    {
        var id = $"HTTP0001";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Unrecognized HTTP verb '{0}'.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, verb);
    }

    internal static HttpDiagnosticInfo MissingUrl()
    {
        var id = $"HTTP0002";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Missing URL.";
        return new HttpDiagnosticInfo(id, messageFormat, severity);
    }

    internal static HttpDiagnosticInfo UnrecognizedUriScheme(string scheme)
    {
        var id = $"HTTP0003";
        var severity = DiagnosticSeverity.Warning;
        var messageFormat = "Unrecognized URI scheme: '{0}'.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, scheme);
    }

    internal static HttpDiagnosticInfo InvalidUri()
    {
        var id = $"HTTP0004";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Invalid URI.";
        return new HttpDiagnosticInfo(id, messageFormat, severity);
    }

    internal static HttpDiagnosticInfo InvalidHttpVersion()
    {
        var id = $"HTTP0005";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Invalid HTTP version.";
        return new HttpDiagnosticInfo(id, messageFormat, severity);
    }

    internal static HttpDiagnosticInfo InvalidWhitespaceInHeaderName()
    {
        var id = $"HTTP0006";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Invalid whitespace in header name.";
        return new HttpDiagnosticInfo(id, messageFormat, severity);
    }

    internal static HttpDiagnosticInfo MissingHeaderName()
    {
        var id = $"HTTP0007";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Missing header name.";
        return new HttpDiagnosticInfo(id, messageFormat, severity);
    }

    internal static HttpDiagnosticInfo MissingHeaderValue()
    {
        var id = $"HTTP0008";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Missing header value.";
        return new HttpDiagnosticInfo(id, messageFormat, severity);
    }

    internal static HttpDiagnosticInfo InvalidHeaderValue(string exceptionMessage)
    {
        var id = $"HTTP0009";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Invalid header value: {0}.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, exceptionMessage);
    }

    internal static HttpDiagnosticInfo VariableNameExpected()
    {
        var id = $"HTTP0010";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Variable name expected.";
        return new HttpDiagnosticInfo(id, messageFormat, severity);
    }

    internal static HttpDiagnosticInfo CannotResolveSymbol(string symbol)
    {
        var id = $"HTTP0011";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Cannot resolve symbol '{0}'.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, symbol);
    }
}
