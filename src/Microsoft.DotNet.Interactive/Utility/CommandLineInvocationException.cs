// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.Utility;

public class CommandLineInvocationException : Exception
{
    public CommandLineInvocationException(CommandLineResult result, string message = null) : base(
            $"""
        {message}
        Exit code {result.ExitCode}
        
        StdErr:
        {string.Join("\n", result.Error)}
        
        StdOut: 
        {string.Join("\n", result.Output)}
        """.Trim())
    {
    }
}