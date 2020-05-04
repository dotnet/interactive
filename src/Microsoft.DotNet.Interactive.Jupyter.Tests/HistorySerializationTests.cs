// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class HistorySerializationTests
    {
        [Fact]
        public void Can_serialize_input_and_output_element()
        {
            var element = new InputOutputHistoryElement(0, 0, "input value", "output result");
            var serialized = JsonConvert.SerializeObject(element);
            serialized.Should().Be(@"[0,0,[""input value"",""output result""]]");
        }

        [Fact]
        public void Can_deserialize_input_and_output_element()
        {
            
            var serialized = @"[0,0,[""input value"",""output result""]]";
            var element = JsonConvert.DeserializeObject<InputOutputHistoryElement>(serialized);
            element.Should().BeEquivalentTo(new InputOutputHistoryElement(0, 0, "input value", "output result"));
        }

        [Fact]
        public void Can_serialize_input_element()
        {
            var element = new InputHistoryElement(0, 0, "input value");
            var serialized = JsonConvert.SerializeObject(element);
            serialized.Should().Be(@"[0,0,""input value""]");
        }

        [Fact]
        public void Can_deserialize_input_element()
        {

            var serialized = @"[0,0,""input value""]";
            var element = JsonConvert.DeserializeObject<InputHistoryElement>(serialized);
            element.Should().BeEquivalentTo(new InputHistoryElement(0, 0, "input value"));
        }

        [Fact]
        public void Can_serialize_history_reply()
        {

            var historyReply = new HistoryReply(new HistoryElement[]
            {
                new InputHistoryElement(0, 0, "input value"),
                new InputHistoryElement(1, 0, "input value"),
                new InputOutputHistoryElement(2, 0, "input value", "output result")
            });

            var serialized = JsonConvert.SerializeObject(historyReply);
            serialized.Should().Be(@"{""history"":[[0,0,""input value""],[1,0,""input value""],[2,0,[""input value"",""output result""]]]}");
        }

        [Fact]
        public void Can_deserialize__history_reply()
        {

            var serialized = @"{""history"":[[0,0,""input value""],[1,0,""input value""],[2,0,[""input value"",""output result""]]]}";
            var historyReply = JsonConvert.DeserializeObject<HistoryReply>(serialized);
            historyReply.Should().BeEquivalentTo(new HistoryReply(new HistoryElement[]
            {
                new InputHistoryElement(0, 0, "input value"),
                new InputHistoryElement(1, 0, "input value"),
                new InputOutputHistoryElement(2, 0, "input value", "output result")
            }));
        }
    }
}