// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting;

internal class DefaultJsonFormatterSet
{
    internal static readonly ITypeFormatter[] DefaultFormatters =
    {
        new JsonFormatter<string>((s, context) =>
        {
            var data = JsonSerializer.Serialize(s, JsonFormatter.SerializerOptions);
            context.Writer.Write(data);
            return true;
        }),
        new JsonFormatter<object>()
    };
}