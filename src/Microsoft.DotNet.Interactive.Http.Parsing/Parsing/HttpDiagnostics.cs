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

    internal static HttpDiagnosticInfo UnableToEvaluateExpression(string symbol)
    {
        var id = $"HTTP0012";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "Unable to evaluate expression '{0}'.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, symbol);
    }

    internal static HttpDiagnosticInfo IncorrectDateTimeFormat(string expression, string dateTimeType)
    {
        var id = $"HTTP0013";
        var severity = DiagnosticSeverity.Error;
        var messageFormat =
            """The supplied expression '{0}' does not follow the correct pattern. The expression should adhere to the following pattern: '{{{{{1} [rfc1123|iso8601|"custom format"] [offset option]}}}}' where offset (if specified) must be a valid integer and option must be one of the following: ms, s, m, h, d, w, M, Q, y. See https://aka.ms/http-date-time-format for more details.""";
        return new HttpDiagnosticInfo(id, messageFormat, severity, expression, dateTimeType);
    }

    internal static HttpDiagnosticInfo IncorrectTimestampFormat(string timestamp)
    {
        var id = $"HTTP0014";
        var severity = DiagnosticSeverity.Error;
        var messageFormat =
            "The supplied expression '{0}' does not follow the correct pattern. The expression should adhere to the following pattern: '{{{{$timestamp [offset option]}}}}' where offset (if specified) must be a valid integer and option must be one of the following: ms, s, m, h, d, w, M, Q, y. See https://aka.ms/http-date-time-format for more details.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, timestamp);
    }

    internal static HttpDiagnosticInfo InvalidOffset(string expression, string offset)
    {
        var id = $"HTTP0015";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "The supplied offset '{1}' in the expression '{0}' is not a valid integer. See https://aka.ms/http-date-time-format for more details.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, expression, offset);
    }

    internal static HttpDiagnosticInfo InvalidOption(string expression, string option)
    {
        var id = $"HTTP0016";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "The supplied option '{1}' in the expression '{0}' is not supported. The following options are supported: ms, s, m, h, d, w, M, Q, y. See https://aka.ms/http-date-time-format for more details.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, expression, option);
    }

    internal static HttpDiagnosticInfo IncorrectRandomIntFormat(string expression)
    {
        var id = $"HTTP0017";
        var severity = DiagnosticSeverity.Error;
        var messageFormat =
            """The supplied expression '{0}' does not follow the correct pattern. The expression should adhere to the following pattern: '{{{{$randomInt [min] [max]]}}}}' where min and max (if specified) must be valid integers.""";
        return new HttpDiagnosticInfo(id, messageFormat, severity, expression);
    }

    internal static HttpDiagnosticInfo RandomIntMinMustNotBeGreaterThanMax(string expression, string min, string max)
    {
        var id = $"HTTP0018";
        var severity = DiagnosticSeverity.Error;
        var messageFormat =
            """The supplied argument '{1}' in the expression '{0}' must not be greater than the supplied argument '{2}'.""";
        return new(id, messageFormat, severity, expression, min, max);
    }

    internal static HttpDiagnosticInfo InvalidRandomIntArgument(string expression, string argument)
    {
        var id = $"HTTP0019";
        var severity = DiagnosticSeverity.Error;
        var messageFormat = "The supplied argument '{1}' in the expression '{0}' is not a valid integer.";
        return new HttpDiagnosticInfo(id, messageFormat, severity, expression, argument);
    }

    internal static HttpDiagnosticInfo IncorrectDateTimeCustomFormat(string format)
    {
        var id = $"HTTP0020";
        var severity = DiagnosticSeverity.Error;
        var messageFormat =
            """The supplied format '{0}' is invalid.""";
        return new HttpDiagnosticInfo(id, messageFormat, severity, format);
    }
}
