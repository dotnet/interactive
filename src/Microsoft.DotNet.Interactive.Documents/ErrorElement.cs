// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Documents;

public class ErrorElement : InteractiveDocumentOutputElement
{
    public ErrorElement(
        string errorValue,
        string? errorName = "Error",
        string[]? stackTrace = null)
    {
        ErrorName = errorName ?? "Error";
        ErrorValue = errorValue ?? "";
        StackTrace = stackTrace ?? new string[] { };
    }

    public string ErrorName { get; }

    public string ErrorValue { get; }

    public string[] StackTrace { get; }
}