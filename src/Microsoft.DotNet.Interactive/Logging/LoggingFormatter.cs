// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

[TypeFormatterSource(typeof(LoggingFormatterSource))]
internal class LoggingFormatter : LoggingFormatter<object>
{
    internal LoggingFormatter() : base(FormatObject)
    {
    }

    private static bool FormatObject(object instance, FormatContext context)
    {
        if (instance is null)
        {
            context.Writer.Write($"<null>");
        }
        else
        {
            context.Writer.Write(instance.ToString());
        }

        return true;
    }
}
