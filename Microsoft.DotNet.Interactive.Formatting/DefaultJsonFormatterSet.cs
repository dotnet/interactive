// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json.Linq;

namespace Microsoft.DotNet.Interactive.Formatting
{
    internal class DefaultJsonFormatterSet : FormatterSetBase
    {
        protected override bool TryInferFormatter(Type type, out ITypeFormatter formatter)
        {
            if (typeof(JToken).IsAssignableFrom(type))
            {
                formatter = Formatter.Create(
                    type,
                    (value, writer) => writer.Write(value.ToString()),
                    JsonFormatter.MimeType);
                return true;
            }

            formatter = null;
            return false;
        }
    }
}