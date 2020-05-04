// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Utility
{
    public class CommandLineResult
    {
        public CommandLineResult(
            int exitCode,
            IReadOnlyCollection<string> output = null,
            IReadOnlyCollection<string> error = null)
        {
            ExitCode = exitCode;
            Output = output ?? Array.Empty<string>();
            Error = error ?? Array.Empty<string>();
        }

        public int ExitCode { get; }

        public IReadOnlyCollection<string> Output { get; }

        public IReadOnlyCollection<string> Error { get; }

        public void ThrowOnFailure(string message = null)
        {
            if (ExitCode != 0)
            {
                throw new CommandLineInvocationException(
                    this,
                    $"{message ?? string.Empty}{Environment.NewLine}{string.Join(Environment.NewLine, Error.Concat(Output))}");
            }
        }
    }
}