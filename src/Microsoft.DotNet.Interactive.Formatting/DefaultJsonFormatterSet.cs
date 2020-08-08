// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class DefaultJsonFormatterSet 
    {
        static internal readonly ITypeFormatter[] DefaultFormatters =
            new ITypeFormatter[]
            {
                new JsonFormatter<JToken>()
            };
    }
}