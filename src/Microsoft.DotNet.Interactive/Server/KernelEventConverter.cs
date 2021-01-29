// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Server
{
    public class KernelEventConverter : JsonConverter<KernelEvent>
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeof(KernelEvent).IsAssignableFrom(typeToConvert);
        }

        public override KernelEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            EnsureStartObject(reader,typeToConvert);
            throw new NotImplementedException();
        }
    }
}