// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal enum WellKnownHttpDiagnostics
{
    UnrecognizedVerb = 1,
    MissingUrl,
    UnrecognizedUriScheme,
    InvalidUri,
    InvalidHttpVersion,
    InvalidWhitespaceInHeaderName,
    MissingHeaderName,
    MissingHeaderValue,
    InvalidHeaderValue,
    VariableNameExpected,
    CannotResolveSymbol
}
