// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

internal static class LoggerExtensions
{
    internal static Pocket.ConfirmationLogger OnEnterAndConfirmOnExit(
        this Pocket.Logger logger,
        object arg,
        [CallerMemberName] string name = null,
        string message = null,
        Func<(string name, object value)[]> exitArgs = null) =>
            logger.OnEnterAndConfirmOnExit(
                new[] { arg },
                name,
                message,
                exitArgs);

    internal static Pocket.ConfirmationLogger OnEnterAndConfirmOnExit(
        this Pocket.Logger logger,
        object[] args,
        [CallerMemberName] string name = null,
        string message = null,
        Func<(string name, object value)[]> exitArgs = null)
    {
        var formattedArgs = new object[args.Length];

        for (var i = 0; i < args.Length; ++i)
        {
            var arg = args[i];
            object formattedArg;

            if (Formatter.GetPreferredMimeTypesFor(arg.GetType()).Contains(MimeTypes.Logging))
            {
                formattedArg = arg.ToDisplayString(MimeTypes.Logging);
            }
            else
            {
                formattedArg = arg;
            }

            formattedArgs[i] = formattedArg;
        }

        return new Pocket.ConfirmationLogger(
            name,
            logger.Category,
            message,
            exitArgs,
            logOnStart: true,
            formattedArgs);
    }
}
