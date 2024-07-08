// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public static class ValueSharingMessageAssertionExtensions
{
    public static void ShouldContainCommMsgWithValues(this SubscribedList<Messaging.Message> messages, params string[] values)
    {
        messages
            .Should()
            .ContainSingle(m => m.Header.MessageType == JupyterMessageContentTypes.CommMsg)
            .Which
            .Content
            .Should()
            .BeOfType<CommMsg>()
            .Which
            .ShouldContainValues(values);
    }

    public static void ShouldContainValues(this CommMsg commMsg, params string[] values)
    {
        commMsg
            .Data
            .Should()
            .ContainKey("commandOrEvent")
            .WhoseValue
            .ToString()
            .Should()
            .ContainAll(values);
    }
}