// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using System.Collections.Generic;
using System.Text.Json;
using Message = Microsoft.DotNet.Interactive.Jupyter.Messaging.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

[TestClass]
public class JupyterMessageSerializationTests
{
    private readonly TestContext _output;

    public JupyterMessageSerializationTests(TestContext output)
    {
        _output = output;
    }
    
    [TestMethod]
    [DynamicData(nameof(Messages))]
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

        // for each message test with values and null values where it's allowed
        IEnumerable<Message> messages()
        {
            yield return Message.Create(new KernelInfoRequest());

            yield return Message.Create(new KernelInfoReply("protocolVersion", "implementation", null, new LanguageInfo("name", "version", "mimeType", "fileExt")));
            
            // codeMirror setting
            yield return Message.Create(new KernelInfoReply("protocolVersion", "implementation", null,
                new LanguageInfo("name", "version", "mimeType", "fileExt", "pygmentsLexer", null , "nbConvertExplorer"), 
                "banner", null));
            
            yield return Message.Create(new Status(StatusValues.Idle));
            
            yield return Message.Create(new ExecuteRequest("code"));
            
            yield return Message.Create(new ExecuteReplyOk());

        };
    }
}
