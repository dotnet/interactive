// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class JsonFormatterTests : FormatterTestBase
    {
        [Theory]
        [MemberData(nameof(JTokens))]
        public void It_does_not_JSON_encode_JSON_types(JToken jtoken)
        {
            var formatter = JsonFormatter.GetPreferredFormatterFor(jtoken.GetType());

            var writer = new StringWriter();

            formatter.Format(jtoken, writer);

            writer
                .ToString()
                .Should()
                .Be(jtoken.ToString(Newtonsoft.Json.Formatting.None));
        }

        public static IEnumerable<object[]> JTokens()
        {
            return jtokens().Select(t => new object[] { t });

            IEnumerable<JToken> jtokens()
            {
                yield return JToken.FromObject(789);
                
                yield return JToken.FromObject("123");
                
                yield return JToken.FromObject(new[] { 1, 2, 3 });
                
                yield return JToken.FromObject(new { parent = new { Child = new { Age = 5 } } });

                yield return JToken.FromObject(JsonConvert.SerializeObject(new { parent = new { Child = new { Age = 5 } } }));

                yield return JToken.FromObject(new Dictionary<string, object>
                {
                    ["anInt"] = 1,
                    ["aString"] = "456",
                    ["anArray"] = new[] { 1, 2, 3 },
                    ["anObject"] = new { parent = new { child = new { age = 5 } } }
                });
            }
        }
    }
}