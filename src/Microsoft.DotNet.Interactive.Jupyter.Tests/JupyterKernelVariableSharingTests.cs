// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Jupyter.Protocol;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.ValueSharing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public class JupyterKernelVariableSharingTests : JupyterKernelTestBase
{
    private async Task SharedValueShouldBeReturnedBackSame<T>(T expectedValue, string csharpDeclaration, Kernel kernel, TestJupyterConnectionOptions options)
    {
        var result = await kernel.SubmitCodeAsync(csharpDeclaration);
        var events = result.Events;

        events.Should().NotContainErrors();

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        result = await kernel.SubmitCodeAsync($"#!testKernel\n#!share --from csharp x");
        events = result.Events;

        events.Should().NotContainErrors();
        sentMessages.ShouldContainCommMsgWithValues("SendValue", "x");

        result = await kernel.SubmitCodeAsync($"#!share --from testKernel x");
        events = result.Events;

        events
            .Should()
            .NotContainErrors();

        events
            .Should()
            .ContainSingle<ValueProduced>()
            .Which
            .Value
            .Should()
            .Be(expectedValue);
    }

    [Theory]
    [JupyterHttpTestData(KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterHttpTestData(KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData(KernelSpecName = PythonKernelName)]
    [JupyterZMQTestData(KernelSpecName = RKernelName)]
    [JupyterTestData(KernelSpecName = PythonKernelName)]
    [JupyterTestData(KernelSpecName = RKernelName)]
    public async Task can_share_primitives_to_and_from_kernel(JupyterConnectionTestData connectionData)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateCompositeKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

        using (new AssertionScope())
        {
            await SharedValueShouldBeReturnedBackSame((long)2, $"var x = 2;", kernel, options);
            await SharedValueShouldBeReturnedBackSame(int.MinValue, $"int x = {int.MinValue};", kernel, options);
            await SharedValueShouldBeReturnedBackSame(int.MaxValue, $"int x = {int.MaxValue};", kernel, options);
            await SharedValueShouldBeReturnedBackSame(-123456789012345, $"long x = -123456789012345;", kernel, options);
            await SharedValueShouldBeReturnedBackSame(123456789012345, $"long x = 123456789012345;", kernel, options);
            await SharedValueShouldBeReturnedBackSame(true, $"bool x = true;", kernel, options);
            await SharedValueShouldBeReturnedBackSame("hi!", $"var x = \"hi!\";", kernel, options);
            await SharedValueShouldBeReturnedBackSame("hi!", $"string x = \"hi!\";", kernel, options);
            await SharedValueShouldBeReturnedBackSame("", $"string x = \"\";", kernel, options);
            await SharedValueShouldBeReturnedBackSame("«ταБЬℓσ»", $"string x = \"«ταБЬℓσ»\";", kernel, options);
            await SharedValueShouldBeReturnedBackSame(-123456.789, $"double x = -123456.789;", kernel, options);
            await SharedValueShouldBeReturnedBackSame(123456.789, $"double x = 123456.789;", kernel, options);
            await SharedValueShouldBeReturnedBackSame(123.789, $"float x = 123.789f;", kernel, options);
            await SharedValueShouldBeReturnedBackSame(123456.789, $"decimal x = 123456.789M;", kernel, options);
            await SharedValueShouldBeReturnedBackSame("a", $"char x = 'a';", kernel, options);
            await SharedValueShouldBeReturnedBackSame("'", $"char x = '\\'';", kernel, options);
            await SharedValueShouldBeReturnedBackSame((long)123, $"byte x = 123;", kernel, options);
            await SharedValueShouldBeReturnedBackSame((long)123, $"short x = 123;", kernel, options);
            await SharedValueShouldBeReturnedBackSame((long)123, $"sbyte x = 123;", kernel, options);
            await SharedValueShouldBeReturnedBackSame((long)123, $"ushort x = 123;", kernel, options);
            await SharedValueShouldBeReturnedBackSame((long)123456, $"uint x = 123456;", kernel, options);
            await SharedValueShouldBeReturnedBackSame(123456789012345, $"ulong x = 123456789012345;", kernel, options);
        }

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData("df_in_kernel.equals(df_roundtrip)", "True", KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("df_in_kernel.equals(df_roundtrip)", "True", KernelSpecName = PythonKernelName)]
    [JupyterTestData("df_in_kernel.equals(df_roundtrip)", "True", KernelSpecName = PythonKernelName)]
    [JupyterHttpTestData("identical(df_in_kernel, df_roundtrip)", "[1] TRUE", KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("identical(df_in_kernel, df_roundtrip)", "[1] TRUE", KernelSpecName = RKernelName)]
    [JupyterTestData("identical(df_in_kernel, df_roundtrip)", "[1] TRUE", KernelSpecName = RKernelName)]
    public async Task can_share_dataframe_to_from_kernel(JupyterConnectionTestData connectionData, string assertIdentical, string expectedAssertionResult)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateCompositeKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

        var df = JsonDocument.Parse(@"
[
  {
        ""name"": ""Granny Smith apple"",
        ""deliciousness"": 0,
        ""color"":""red""
  },
  {
        ""name"": ""Rainier cherry"",
        ""deliciousness"": 9000,
        ""color"":""yellow""
  }
]").ToTabularDataResource();

        var result = await kernel.SendAsync(new SendValue("df", df, null, "csharp"));
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();

        result = await kernel.SubmitCodeAsync($"#!testKernel\n#!share --from csharp df --as df_in_kernel");
        events = result.Events;

        events
            .Should()
            .NotContainErrors();

        sentMessages.ShouldContainCommMsgWithValues("SendValue", "df_in_kernel");

        sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var dfResult = await kernel.SendAsync(new RequestValue("df_in_kernel", "application/json", "testKernel"));

        dfResult
            .Events
            .Should()
            .NotContainErrors();

        sentMessages.ShouldContainCommMsgWithValues("RequestValue", "df_in_kernel");

        dfResult
            .Events
            .Should()
            .ContainSingle<ValueProduced>()
            .Which
            .Value
            .Should()
            .BeAssignableTo<TabularDataResource>()
            .Which
            .Data
            .Should()
            .BeEquivalentTo(df.Data);

        result = await kernel.SubmitCodeAsync(@"
#!share --from testKernel df_in_kernel --as df_shared
#!testKernel
#!share --from csharp df_shared --as df_roundtrip
");
        events = result.Events;

        events
            .Should()
            .NotContainErrors();

        var csharpDf = await kernel.SendAsync(new RequestValue("df_shared", "application/json", "csharp"));

        csharpDf
            .Events
            .Should()
            .NotContainErrors();

        csharpDf
            .Events
            .Should()
            .ContainSingle<ValueProduced>()
            .Which
            .Value
            .Should()
            .BeAssignableTo<TabularDataResource>()
            .Which
            .Data
            .Should()
            .BeEquivalentTo(df.Data);

        result = await kernel.SubmitCodeAsync($"#!testKernel\n{assertIdentical}");
        events = result.Events;

        events
            .Should()
            .NotContainErrors();

        events
            .Should()
            .ContainSingle<DisplayEvent>()
            .Which
            .FormattedValues
            .Should()
                .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
                .Which
                .Value
                .Should()
                .Be(expectedAssertionResult);
        // kernel was able to validate that round-tripped dataframes were equal

        options.SaveState();
    }

    // for validating the kernel side logic, this test is intended to be run against a jupyter connection that and not just a test connection
    [Theory]
    [JupyterHttpTestData(KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData(KernelSpecName = PythonKernelName)]
    [JupyterTestData(KernelSpecName = PythonKernelName)]
    [JupyterHttpTestData(KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData(KernelSpecName = RKernelName)]
    [JupyterTestData(KernelSpecName = RKernelName)]
    public async Task can_handle_setting_multiple_df_on_kernel(JupyterConnectionTestData connectionData)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = await CreateJupyterKernelAsync(options, connectionData.KernelSpecName, connectionData.GetConnectionString());

        var dfs = new[] {
            JsonDocument.Parse(@"
[
  {
        ""name"": ""Granny Smith apple"",
        ""deliciousness"": 0,
        ""color"":""red""
  },
  {
        ""name"": ""Rainier cherry"",
        ""deliciousness"": 9000,
        ""color"":""yellow""
  }
]").ToTabularDataResource(),
            JsonDocument.Parse(@"
[
  {
        ""a"": ""1"",
        ""b"": 1
  },
  {
        ""a"": ""2"",
        ""b"": 2
  }
]").ToTabularDataResource()
        };

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var sendCommand = new SendValue("df", dfs, new FormattedValue("application/json", null));
        var result = await kernel.SendAsync(sendCommand);
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        var commMessages = sentMessages
            .Where(m => m.Header.MessageType == JupyterMessageContentTypes.CommMsg)
            .ToList();

        commMessages
            .Should()
            .HaveCount(2);

        for (int i = 0; i < commMessages.Count; i++)
        {
            var message = commMessages[i].Content.As<CommMsg>();
            message.ShouldContainValues("SendValue", $"df{i+1}");
        }

        events
            .Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(v => v.MimeType == PlainTextFormatter.MimeType)
                .Which
                .Value
                .Should()
                .ContainAll("df1", "df2");

        var df1Result = await kernel.SendAsync(new RequestValue("df1"));

        df1Result
            .Events
            .Should()
            .NotContainErrors();

        df1Result
            .Events
            .Should()
            .ContainSingle<ValueProduced>()
            .Which
            .Value
            .Should()
            .BeAssignableTo<TabularDataResource>()
            .Which
            .Data
            .Should()
            .BeEquivalentTo(dfs[0].Data);

        var df2Result = await kernel.SendAsync(new RequestValue("df2"));

        df2Result
            .Events
            .Should()
            .NotContainErrors();

        df2Result
            .Events
            .Should()
            .ContainSingle<ValueProduced>()
            .Which
            .Value
            .Should()
            .BeAssignableTo<TabularDataResource>()
            .Which
            .Data
            .Should()
            .BeEquivalentTo(dfs[1].Data);

        options.SaveState();
    }

    // for validating the kernel side logic, this test is intended to be run against a jupyter connection that and not just a test connection
    [Theory]
    [JupyterHttpTestData(KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData(KernelSpecName = PythonKernelName)]
    [JupyterHttpTestData(KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData(KernelSpecName = RKernelName)]
    [JupyterTestData(KernelSpecName = PythonKernelName)]
    [JupyterTestData(KernelSpecName = RKernelName)]
    public async Task can_handle_setting_single_df_in_enumerable_on_kernel(JupyterConnectionTestData connectionData)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = await CreateJupyterKernelAsync(options, connectionData.KernelSpecName, connectionData.GetConnectionString());

        var dfs = new[] {
            JsonDocument.Parse(@"
[
  {
        ""name"": ""Granny Smith apple"",
        ""deliciousness"": 0,
        ""color"":""red""
  },
  {
        ""name"": ""Rainier cherry"",
        ""deliciousness"": 9000,
        ""color"":""yellow""
  }
]").ToTabularDataResource()
        };

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var sendCommand = new SendValue("df", dfs, new FormattedValue("application/json", null));
        var result = await kernel.SendAsync(sendCommand);
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        events
            .Should()
            .NotContain(e => e is DisplayedValueProduced);

        sentMessages.ShouldContainCommMsgWithValues("SendValue", "df");

        var dfResult = await kernel.SendAsync(new RequestValue("df"));

        dfResult
            .Events
            .Should()
            .NotContainErrors();

        dfResult
            .Events
            .Should()
            .ContainSingle<ValueProduced>()
            .Which
            .Value
            .Should()
            .BeAssignableTo<TabularDataResource>()
            .Which
            .Data
            .Should()
            .BeEquivalentTo(dfs[0].Data);

        options.SaveState();
    }

    // for validating the kernel side logic, this test is intended to be run against a jupyter connection that and not just a test connection
    [Theory]
    [JupyterHttpTestData(KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData(KernelSpecName = PythonKernelName)]
    [JupyterHttpTestData(KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData(KernelSpecName = RKernelName)]
    [JupyterTestData(KernelSpecName = PythonKernelName)]
    [JupyterTestData(KernelSpecName = RKernelName)]
    public async Task can_handle_setting_single_df_on_kernel(JupyterConnectionTestData connectionData)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = await CreateJupyterKernelAsync(options, connectionData.KernelSpecName, connectionData.GetConnectionString());

        var df = JsonDocument.Parse(@"
[
  {
        ""name"": ""Granny Smith apple"",
        ""deliciousness"": 0,
        ""color"":""red""
  },
  {
        ""name"": ""Rainier cherry"",
        ""deliciousness"": 9000,
        ""color"":""yellow""
  }
]").ToTabularDataResource();

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var sendCommand = new SendValue("df", df, new FormattedValue("application/json", null));
        var result = await kernel.SendAsync(sendCommand);
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        events
            .Should()
            .NotContain(e => e is DisplayedValueProduced);

        sentMessages.ShouldContainCommMsgWithValues("SendValue", "df");

        var dfResult = await kernel.SendAsync(new RequestValue("df"));

        dfResult
            .Events
            .Should()
            .NotContainErrors();

        dfResult
            .Events
            .Should()
            .ContainSingle<ValueProduced>()
            .Which
            .Value
            .Should()
            .BeAssignableTo<TabularDataResource>()
            .Which
            .Data
            .Should()
            .BeEquivalentTo(df.Data);

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData("a.b", KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterHttpTestData("_ab", KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("a.b", KernelSpecName = PythonKernelName)]
    [JupyterZMQTestData("_ab", KernelSpecName = RKernelName)]
    [JupyterTestData("a.b", KernelSpecName = PythonKernelName)]
    [JupyterTestData("_ab", KernelSpecName = RKernelName)]
    public async Task can_handle_errors_for_send_value_from_kernel(JupyterConnectionTestData connectionData, string invalidId)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = await CreateJupyterKernelAsync(options, connectionData.KernelSpecName, connectionData.GetConnectionString());

        var sendCommand = new SendValue(invalidId, "1", new FormattedValue("application/json", "1"));
        var result = await kernel.SendAsync(sendCommand);
        var events = result.Events;

        events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .ContainAll("Invalid Identifier", invalidId);

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData(KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterHttpTestData(KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData(KernelSpecName = PythonKernelName)]
    [JupyterZMQTestData(KernelSpecName = RKernelName)]
    [JupyterTestData(KernelSpecName = PythonKernelName)]
    [JupyterTestData(KernelSpecName = RKernelName)]
    public async Task can_handle_errors_for_request_value_from_kernel(JupyterConnectionTestData connectionData)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = await CreateJupyterKernelAsync(options, connectionData.KernelSpecName, connectionData.GetConnectionString());

        var result = await kernel.SendAsync(new RequestValue("unknownVar"));
        var events = result.Events;

        events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .ContainAll("not found", "unknownVar");

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData("a = 12345", new[] { "a", "b", "df" }, new[] { "application/json", "application/json", "application/table-schema+json" }, new[] { "12345", "6789", "              name  deliciousness  color\nGranny Smith apple              0    red\n    Rainier cherry           9000 yellow" }, new[] { "<class \'int\'>", "<class \'int\'>", "<class \'pandas.core.frame.DataFrame\'>" }, KernelSpecName = PythonKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("a = 12345", new[] { "a", "b", "df" }, new[] { "application/json", "application/json", "application/table-schema+json" }, new[] { "12345", "6789", "              name  deliciousness  color\nGranny Smith apple              0    red\n    Rainier cherry           9000 yellow" }, new[] { "<class \'int\'>", "<class \'int\'>", "<class \'pandas.core.frame.DataFrame\'>" }, KernelSpecName = PythonKernelName)]
    [JupyterTestData("a = 12345", new[] { "a", "b", "df" }, new[] { "application/json", "application/json", "application/table-schema+json" }, new[] { "12345", "6789", "              name  deliciousness  color\nGranny Smith apple              0    red\n    Rainier cherry           9000 yellow" }, new[] { "<class \'int\'>", "<class \'int\'>", "<class \'pandas.core.frame.DataFrame\'>" }, KernelSpecName = PythonKernelName)]
    [JupyterHttpTestData("a <- 12345", new[] { "a", "b", "df" }, new[] { "application/json", "application/json", "application/table-schema+json" }, new[] { "12345", "6789", "[{\"name\":\"Granny Smith apple\",\"deliciousness\":0,\"color\":\"red\"},{\"name\":\"Rainier cherry\",\"deliciousness\":9000,\"color\":\"yellow\"}]" }, new[] { "double", "integer", "data.frame" }, KernelSpecName = RKernelName, AllowPlayback = RECORD_FOR_PLAYBACK)]
    [JupyterZMQTestData("a <- 12345", new[] { "a", "b", "df" }, new[] { "application/json", "application/json", "application/table-schema+json" }, new[] { "12345", "6789", "[{\"name\":\"Granny Smith apple\",\"deliciousness\":0,\"color\":\"red\"},{\"name\":\"Rainier cherry\",\"deliciousness\":9000,\"color\":\"yellow\"}]" }, new[] { "double", "integer", "data.frame" }, KernelSpecName = RKernelName)]
    [JupyterTestData("a <- 12345", new[] { "a", "b", "df" }, new[] { "application/json", "application/json", "application/table-schema+json" }, new[] { "12345", "6789", "[{\"name\":\"Granny Smith apple\",\"deliciousness\":0,\"color\":\"red\"},{\"name\":\"Rainier cherry\",\"deliciousness\":9000,\"color\":\"yellow\"}]" }, new[] { "double", "integer", "data.frame" }, KernelSpecName = RKernelName)]
    public async Task can_request_value_infos_for_shared_and_kernel_variables(JupyterConnectionTestData connectionData, string kernelVarDeclare, string[] variables, string[] mimeTypes, string[] formattedValues, string[] typeNames)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = await CreateJupyterKernelAsync(options, connectionData.KernelSpecName, connectionData.GetConnectionString());

        // setting variable on kernel
        await kernel.SubmitCodeAsync(kernelVarDeclare);

        // share a variable
        await kernel.SendAsync(new SendValue("b", 6789, new FormattedValue("application/json", "6789")));

        var df = JsonDocument.Parse(@"
[
  {
        ""name"": ""Granny Smith apple"",
        ""deliciousness"": 0,
        ""color"":""red""
  },
  {
        ""name"": ""Rainier cherry"",
        ""deliciousness"": 9000,
        ""color"":""yellow""
  }
]").ToTabularDataResource();

        // share a dataframe
        await kernel.SendAsync(new SendValue("df", df,
            new FormattedValue(TabularDataResourceFormatter.MimeType,
                        JsonSerializer.Serialize(df, TabularDataResourceFormatter.JsonSerializerOptions))));

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var results = await kernel.SendAsync(new RequestValueInfos());
        var events = results.Events;

        events
            .Should()
            .NotContainErrors();

        sentMessages.ShouldContainCommMsgWithValues("RequestValueInfos");

        var valueInfos = events
            .Should()
            .ContainSingle<ValueInfosProduced>()
            .Which
            .ValueInfos;

        valueInfos
            .Select(v => v.Name)
            .Should()
            .Contain("a", "b", "df");

        Assert.Equal(variables.Length, valueInfos.Count());

        for (int i = 0; i < variables.Length; i++)
        {
            valueInfos
                .First(v => v.Name == variables[i])
                .Should()
                .BeEquivalentTo(
                    new KernelValueInfo(variables[i],
                            new FormattedValue(mimeTypes[i], formattedValues[i]),
                            null, typeNames[i]));
        }
        options.SaveState();
    }
}
