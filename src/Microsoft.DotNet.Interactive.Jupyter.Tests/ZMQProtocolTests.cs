// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Jupyter.Messaging;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class ZMQProtocolTests
{
    [Fact]
    public void can_deserialize_execute_request()
    {
        var signature = "test";

        var headerJson = "{\"date\":\"2021-01-29T13:30:01.042Z\",\"msg_id\":\"5687f9e8-ee7e-427e-8e67-935c66448627\",\"msg_type\":\"execute_request\",\"session\":\"28b3fafe-aec1-4272-85e5-a6806d949d66\",\"username\":\"\",\"version\":\"5.2\"}";

        var parentHeaderJson = "{}";

        var metadataJson = "{\"deletedCells\":[],\"recordTiming\":false,\"cellId\":\"4fa8adf4-bdd2-4e92-bed2-871fb4e17fd2\"}";

        var contentJson = "{\"silent\":false,\"store_history\":true,\"user_expressions\":{},\"allow_stdin\":true,\"stop_on_error\":true,\"code\":\"1+2\\n\"}";

        var identifiers = new List<byte[]>();

        var message = MessageExtensions.DeserializeMessage(signature, headerJson,parentHeaderJson,  metadataJson, contentJson, identifiers);

        message.Header.Should().NotBeNull();
        message.Content.Should().NotBeNull().And.BeOfType<ExecuteRequest>();
        message.MetaData.Should().BeEquivalentTo(new Dictionary<string, object>
        {
            ["deletedCells"] = new object[0],
            ["recordTiming"] = false,
            ["cellId"] = "4fa8adf4-bdd2-4e92-bed2-871fb4e17fd2"
        });
    }

    [Fact]
    public void can_deserialize_kernel_info_request()
    {
        var signature = "test" ;

        var headerJson = "{\"msg_id\":\"fb0c43f5-7aa3-4acc-9920-bb813e587fd6_0\",\"msg_type\":\"kernel_info_request\",\"username\":\"username\",\"session\":\"fb0c43f5-7aa3-4acc-9920-bb813e587fd6\",\"date\":\"2021-01-29T13:17:07.807175Z\",\"version\":\"5.3\"}";

        var parentHeaderJson = "{}";

        var metadataJson = "{}";

        var contentJson = "{}";

        var identifiers = new List<byte[]>();

        var message = MessageExtensions.DeserializeMessage(signature, headerJson, parentHeaderJson, metadataJson, contentJson, identifiers);
            
        using var _ = new AssertionScope();
            
        message.Header.Should().NotBeNull();
        message.Content.Should().NotBeNull();
    }
}