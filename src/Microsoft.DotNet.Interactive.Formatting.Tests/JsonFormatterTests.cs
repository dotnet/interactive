// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using System.Linq;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

[TestClass]
public class JsonFormatterTests : FormatterTestBase
{
    [TestMethod]
    [DynamicData(nameof(JsonElements), UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Fold)]
    public void It_does_not_JSON_encode_JSON_types(JsonElement jsonElement)
    {
        var formatter = JsonFormatter.GetPreferredFormatterFor(jsonElement.GetType());

        var writer = new StringWriter();

        formatter.Format(jsonElement, writer);

        writer
            .ToString()
            .Should()
            .Be(JsonSerializer.Serialize(jsonElement));
    }

    public static IEnumerable<object[]> JsonElements()
    {
        return elements().Select(t => new object[] { t });

        IEnumerable<JsonElement> elements()
        {
            yield return JsonDocument.Parse( JsonSerializer.Serialize(789)).RootElement;
                
            yield return JsonDocument.Parse(JsonSerializer.Serialize("123")).RootElement;
                
            yield return JsonDocument.Parse(JsonSerializer.Serialize(new[] { 1, 2, 3 })).RootElement; 
                
            yield return JsonDocument.Parse(JsonSerializer.Serialize(new { parent = new { Child = new { Age = 5 } } })).RootElement; 
                
            yield return JsonDocument.Parse(JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["anInt"] = 1,
                ["aString"] = "456",
                ["anArray"] = new[] { 1, 2, 3 },
                ["anObject"] = new { parent = new { child = new { age = 5 } } }
            })).RootElement; 
        }
    }
}