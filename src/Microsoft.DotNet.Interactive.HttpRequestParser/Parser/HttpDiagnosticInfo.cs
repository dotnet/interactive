// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal sealed class HttpDiagnosticInfo
{
    internal string Id { get; }
    internal string MessageFormat { get; }
    internal DiagnosticSeverity Severity { get; }
    internal object[] MessageArguments { get; }

    internal HttpDiagnosticInfo(
        string id,
        string messageFormat,
        DiagnosticSeverity severity,
        params object[] messageArguments)
    {
        Id = id;
        MessageFormat = messageFormat;
        Severity = severity;
        MessageArguments = messageArguments;
    }
}
