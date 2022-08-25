// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Messaging
{
    internal static class MessageFormatter
    {
        static MessageFormatter()
        {
            SerializerOptions = JsonFormatter.SerializerOptions;
            SerializerOptions.Converters.Add(new MessageConverter());
        }

        public static JsonSerializerOptions SerializerOptions { get; }
    }
}
