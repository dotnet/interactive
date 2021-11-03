// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive
{
    internal class DataExplorerFormatterSource : ITypeFormatterSource
    {
        public IEnumerable<ITypeFormatter> CreateTypeFormatters()
        {
            yield return new JsonFormatter<DataExplorer<TabularDataResource>>((value, context) =>
            {
                var json = JsonSerializer.Serialize(value.Data.Data,
                    TabularDataResourceFormatter.JsonSerializerOptions);

                context.Writer.Write(json);
            });

            yield return new TabularDataResourceFormatter<DataExplorer<TabularDataResource>>((value, context) =>
            {
                var json = JsonSerializer.Serialize(value.Data, TabularDataResourceFormatter.JsonSerializerOptions);

                context.Writer.Write(json);
            });
        }
    }
}