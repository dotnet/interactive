// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.HttpRequest;

internal sealed class Error
{
    public Error(string errorCode, string message, ErrorCategory severity)
    {
        ErrorCode = errorCode;
        Message = message;
        Severity = severity;
    }

    public string ErrorCode { get; }
    public string Message { get; }
    public ErrorCategory Severity { get; }

    public Error WithFormat(params string[] replacements)
    {
        return new Error(ErrorCode, string.Format(Message, replacements), Severity);
    }
}
