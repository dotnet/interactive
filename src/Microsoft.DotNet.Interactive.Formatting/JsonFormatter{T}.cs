// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class JsonFormatter<T> : TypeFormatter<T>
    {
        public override bool Format(FormatContext context, T instance, TextWriter writer)
        {
            var json = JsonConvert.SerializeObject(instance, JsonFormatter.SerializerSettings);

            writer.Write(json);
            return true;
        }

        public override string MimeType { get; } = JsonFormatter.MimeType;
    }
}