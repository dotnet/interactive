// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.Interactive.Parsing;

internal sealed class DiagnosticInfo
{
    internal DiagnosticInfo(
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

    internal string Id { get; }

    internal string MessageFormat { get; }

    internal DiagnosticSeverity Severity { get; }

    internal object[] MessageArguments { get; }
<<<<<<<< HEAD:src/Microsoft.DotNet.Interactive/Parsing/DiagnosticInfo.cs
}