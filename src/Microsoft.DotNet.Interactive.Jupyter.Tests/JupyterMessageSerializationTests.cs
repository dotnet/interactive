using FluentAssertions;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class JupyterMessageSerializationTests
{
    private readonly ITestOutputHelper _output;

    public JupyterMessageSerializationTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    [Theory]
    [MemberData(nameof(Messages))]
    public void All_message_types_are_round_trip_serializable(Message message)
    {
        var json = JsonSerializer.Serialize(message, MessageFormatter.SerializerOptions);
        
        _output.WriteLine(json);

        var deserializedMessage = JsonSerializer.Deserialize<Message>(json, MessageFormatter.SerializerOptions);

        deserializedMessage
            .Should()
            .BeEquivalentToRespectingRuntimeTypes(message);
    }

    public static IEnumerable<object[]> Messages()
    {
        foreach (var message in messages())
        {
            yield return new object[] { message };
        }
        IEnumerable<Message> messages()
        {
            yield return Message.Create(new ExecuteReplyOk());
        };
    }
}
