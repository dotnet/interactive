// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.Http.Parsing;

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

    internal static HttpDiagnosticInfo CannotSetContentHeaderWithoutContent(string headerText)
    {
        var id = $"HTTP0009";
        var severity = DiagnosticSeverity.Warning;
        var messageFormat = "Header '{0}' will be ignored: Cannot set content header without content.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, headerText);
    }

    internal static HttpDiagnosticInfo InvalidHeader(string headerText, string exceptionMessage)
    {
        var id = $"HTTP0010";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Invalid header '{0}': {1}";
        return new HttpDiagnosticInfo(id, messageFormat, severity, headerText, exceptionMessage);
    }

    internal static HttpDiagnosticInfo VariableNameExpected()
    {
        var id = $"HTTP0011";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Variable name expected.";
        return new HttpDiagnosticInfo(id, messageFormat, severity);
    }

    internal static HttpDiagnosticInfo CannotResolveSymbol(string symbol)
    {
        var id = $"HTTP0012";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Cannot resolve symbol '{0}'.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, symbol);
    }

    internal static HttpDiagnosticInfo DateTimePatternMatchError(string datetime)
    {
        var id = $"HTTP0013";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = 
            """
            Error in pattern.

            Usage: {{{0} rfc1123|iso8601|"custom format" [offset option]}}

            You can specify a date time relative to the current date like: {{{0} rfc1123 3 M}} to represent 3 months later in RFC1123 format.
            """;
        return new HttpDiagnosticInfo(id, messageFormat, severity, datetime);
    }

    internal static HttpDiagnosticInfo InvalidFormat(string format)
    {
        var id = $"HTTP0014";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "The format string '{0}' is invalid.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, format);
    }

    internal static HttpDiagnosticInfo InvalidOffset(string offset)
    {
        var id = $"HTTP0015";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "An error occurred while applying the offset '{0}'.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, offset);
    }

    internal static HttpDiagnosticInfo TimestampFormatError(string timestamp)
    {
        var id = $"HTTP0016";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = """
            Error in pattern.

            Usage: {{$timestamp [offset option]}}

            You can specify an offset relative to the current timestamp like: {{$timestamp 3 M}} to represent a timestamp 3 months later.
            """;
        return new HttpDiagnosticInfo(id, messageFormat, severity, timestamp);
    }

    public static HttpDiagnosticInfo RandomIntMustBeGreaterThanOrEqualToZero() {
        var id = $"HTTP0017";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = """
            Parameter must be greater than or equal to 0.
            """;
        return new HttpDiagnosticInfo(id, messageFormat, severity);
    }
    public static HttpDiagnosticInfo RandomIntMustBeIntegerArgument(string value)
    {
        var id = $"HTTP0018";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = """
            The parameter "{0}" must be a valid integer value.
            """;
        return new (id, messageFormat, severity, value);
    } 
    public static HttpDiagnosticInfo RandomIntInvalidArguments(string max, string min)
    {
        var id = $"HTTP0019";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = """
            The parameter "{0}" must not be greater than the parameter "{1}".
            """;
        return new (id, messageFormat, severity, [max, min]);
    } 
    public static HttpDiagnosticInfo NoPatternMatch(string text, string pattern) {
        var id = $"HTTP0020";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = """
            The text "{0}" did not match the pattern, "{1}".
            """;
        return new(id,messageFormat, severity, [text, pattern]);
    }

}
