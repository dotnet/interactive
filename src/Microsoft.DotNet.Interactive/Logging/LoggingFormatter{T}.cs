// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

internal class LoggingFormatter<T> : TypeFormatter<T>
{
    private readonly FormatDelegate<T> _format;

    internal LoggingFormatter(FormatDelegate<T> format) =>
        _format = format;

    internal LoggingFormatter(Action<T, FormatContext> format) =>
        _format = (instance, context) =>
        {
            format(instance, context);
            return true;
        };

    internal LoggingFormatter(Func<T, string> format) =>
        _format = (instance, context) =>
        {
            context.Writer.Write(format(instance));
            return true;
        };

    public override string MimeType => MimeTypes.Logging;

    public override bool Format(T value, FormatContext context)
    {
        if (value is null)
        {
            context.Writer.Write($"{typeof(T).Name}: <null>");
            return true;
        }

        return _format(value, context);
    }
}
