// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis.Scripting;

namespace Microsoft.DotNet.Interactive.CSharpProject
{
    public static class ExceptionExtensions
    {
        public static string ToDisplayString(this Exception exception)
        {
            switch (exception)
            {
                case CompilationErrorException _:
                    return null;

                default:
                    return exception?.ToString();
            }
        }

        public static bool IsConsideredRunFailure(this Exception exception) =>
            exception is TimeoutException ||
            exception is CompilationErrorException;
    }
}
