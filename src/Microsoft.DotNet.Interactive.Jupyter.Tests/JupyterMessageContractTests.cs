// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Assent;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Jupyter.ZMQ;
using Xunit;
using Message = Microsoft.DotNet.Interactive.Jupyter.ZMQ.Message;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests
{
    public class JupyterMessageContractTests
    {
        private readonly Configuration _configuration;

        public JupyterMessageContractTests()
        {
            _configuration = new Configuration()
                .UsingExtension("json");

            _configuration = _configuration.SetInteractive(true);
        }

        [Fact]
        public void KernelInfoReply_contract_has_not_been_broken()
        {
            var socket = new TextSocket();
            var sender = new MessageSender(socket, new SignatureValidator("key", "HMACSHA256"));
            var kernelInfoReply = new KernelInfoReply(
                                      Constants.MESSAGE_PROTOCOL_VERSION,
                                      ".NET",
                                      "0.0.3",
                                       new LanguageInfo(
                                           name: "C#",
                                           version: typeof(string).Assembly.ImageRuntimeVersion.Substring(1),
                                           mimeType: "text/x-csharp",
                                           fileExtension: ".cs",
                                           pygmentsLexer: "c#"
                                       ));
            var header = new Header(messageType: JupyterMessageContentTypes.KernelInfoReply, messageId: Guid.Empty.ToString(),
                version: Constants.MESSAGE_PROTOCOL_VERSION, username: Constants.USERNAME, session: "test session",
                date: DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            var replyMessage = new Message(header, content: kernelInfoReply);
            sender.Send(replyMessage);

            var encoded = socket.GetEncodedMessage();
            this.Assent(encoded, _configuration);
        }

        [Fact]
        public void Execute_result_contract_has_not_been_broken()
        {
            var socket = new TextSocket();
            var sender = new MessageSender(socket, new SignatureValidator("key", "HMACSHA256"));
            var transient = new Dictionary<string, object> { { "display_id", "none" } };
            var output = "some result";
            var executeResult = new ExecuteResult(
                12,
                transient: transient,
                data: new Dictionary<string, object> {
                    { "text/html", output },
                    { "text/plain", output }

                });

            var header = new Header(messageType: JupyterMessageContentTypes.ExecuteResult, messageId: Guid.Empty.ToString(),
                version: "5.3", username: Constants.USERNAME, session: "test session",
                date: DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            var replyMessage = new Message(header, content: executeResult);

            sender.Send(replyMessage);

            var encoded = socket.GetEncodedMessage();
            this.Assent(encoded, _configuration);
        }

        [Fact]
        public void Display_data_contract_has_not_been_broken()
        {
            var socket = new TextSocket();
            var sender = new MessageSender(socket, new SignatureValidator("key", "HMACSHA256"));
            var transient = new Dictionary<string, object> { { "display_id", "none" } };
            var output = "some result";
            var displayData = new DisplayData(
                data: new Dictionary<string, object>
                {
                    {"text/html", output},
                    {"text/plain", output}
                },
                transient: transient);


            var header = new Header(messageType: JupyterMessageContentTypes.DisplayData, messageId: Guid.Empty.ToString(),
                version: "5.3", username: Constants.USERNAME, session: "test session",
                date: DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            var replyMessage = new Message(header, content: displayData);

            sender.Send(replyMessage);

            var encoded = socket.GetEncodedMessage();
            this.Assent(encoded, _configuration);
        }

        [Fact]
        public void Complete_reply_contract_has_not_been_broken()
        {
            var socket = new TextSocket();
            var sender = new MessageSender(socket, new SignatureValidator("key", "HMACSHA256"));

            var completeReply = new CompleteReply(0, 0, matches: new List<string> { "Write", "WriteLine" });

            var header = new Header(messageType: JupyterMessageContentTypes.CompleteReply, messageId: Guid.Empty.ToString(),
                version: "5.3", username: Constants.USERNAME, session: "test session",
                date: DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            var replyMessage = new Message(header, content: completeReply);
            sender.Send(replyMessage);

            var encoded = socket.GetEncodedMessage();
            this.Assent(encoded, _configuration);
        }

        [Fact]
        public void Update_data_contract_has_not_been_broken()
        {
            var socket = new TextSocket();
            var sender = new MessageSender(socket, new SignatureValidator("key", "HMACSHA256"));
            var transient = new Dictionary<string, object> { { "display_id", "none" } };
            var output = "some result";
            var displayData = new UpdateDisplayData
            (
                data: new Dictionary<string, object>
                {
                    { "text/html", output },
                    { "text/plain", output }
                },
                transient: transient
            );

            var header = new Header(messageType: JupyterMessageContentTypes.UpdateDisplayData, messageId: Guid.Empty.ToString(),
                version: "5.3", username: Constants.USERNAME, session: "test session",
                date: DateTime.MinValue.ToString("yyyy-MM-ddTHH:mm:ssZ"));

            var replyMessage = new Message(header, content: displayData);
            sender.Send(replyMessage);

            var encoded = socket.GetEncodedMessage();
            this.Assent(encoded, _configuration);
        }
    }
}