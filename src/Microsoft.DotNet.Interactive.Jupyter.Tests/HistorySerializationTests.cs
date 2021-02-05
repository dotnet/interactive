// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

using FluentAssertions;

using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class HistorySerializationTests
    {
        [Fact]
        public void Can_serialize_history_reply()
        {

            var historyReply = new HistoryReply(new HistoryElement[]
            {
                new InputHistoryElement(0, 0, "input value"),
                new InputHistoryElement(1, 0, "input value"),
                new InputOutputHistoryElement(2, 0, "input value", "output result")
            });

            var serialized = JsonSerializer.Serialize(historyReply);
            serialized.Should().Be(@"{""history"":[[0,0,""input value""],[1,0,""input value""],[2,0,[""input value"",""output result""]]]}");
        }

        [Fact]
        public void Can_serialize_empty_history_reply()
        {
            var historyReply = new HistoryReply(new HistoryElement[]{});

            var serialized = JsonSerializer.Serialize(historyReply);
            serialized.Should().Be(@"{""history"":[]}");
        }

        [Fact]
        public void Can_serialize_null_history_reply()
        {
            var historyReply = new HistoryReply();

            var serialized = JsonSerializer.Serialize(historyReply);
            serialized.Should().Be(@"{""history"":[]}");
        }

        [Fact]
        public void Can_deserialize_history_reply()
        {

            var serialized = @"{""history"":[[0,0,""input value""],[1,0,""input value""],[2,0,[""input value"",""output result""]]]}";
            var historyReply = JsonSerializer.Deserialize<HistoryReply>(serialized);
            historyReply.Should().BeEquivalentToRespectingRuntimeTypes(new HistoryReply(new HistoryElement[]
            {
                new InputHistoryElement(0, 0, "input value"),
                new InputHistoryElement(1, 0, "input value"),
                new InputOutputHistoryElement(2, 0, "input value", "output result")
            }));
        }

        [Fact]
        public void Can_deserialize_empty_history_reply()
        {

            var serialized = @"{""history"":[]}";
            var historyReply = JsonSerializer.Deserialize<HistoryReply>(serialized);
            historyReply.Should().BeEquivalentToRespectingRuntimeTypes(new HistoryReply(new HistoryElement[]{}));
        }

        [Fact]
        public void Can_deserialize_null_history_reply()
        {
            var serialized = @"{""history"":null}";
            var historyReply = JsonSerializer.Deserialize<HistoryReply>(serialized);
            historyReply.Should().BeEquivalentToRespectingRuntimeTypes(new HistoryReply());
        }
    }
}