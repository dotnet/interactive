// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Tests.Utility;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.DotNet.Interactive.Jupyter.Tests;

public partial class JupyterKernelTests : IDisposable
{
    private async Task SharedValueShouldBeReturnedBackSame<T>(T expectedValue, string csharpDeclaration, Kernel kernel, TestJupyterConnectionOptions options)
    {
        var result = await kernel.SubmitCodeAsync(csharpDeclaration);
        var events = result.Events;

        events.Should().NotContainErrors();

        result = await kernel.SubmitCodeAsync($"#!testKernel\n#!share --from csharp x");
        events = result.Events;

        events.Should().NotContainErrors();

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

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
    [JupyterHttpTestData(KernelSpecName = PythonKernelName, AllowPlayback = false)]
    [JupyterHttpTestData(KernelSpecName = RKernelName, AllowPlayback = false)]
    public async Task can_share_primitives_to_and_from_kernel(JupyterConnectionTestData connectionData)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateCompositeKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");

        await SharedValueShouldBeReturnedBackSame((long)2, $"var x = 2;", kernel, options);
        await SharedValueShouldBeReturnedBackSame(int.MinValue, $"int x = {int.MinValue};", kernel, options);
        await SharedValueShouldBeReturnedBackSame(int.MaxValue, $"int x = {int.MaxValue};", kernel, options);
        await SharedValueShouldBeReturnedBackSame(-123456789012345, $"long x = -123456789012345;", kernel, options);
        await SharedValueShouldBeReturnedBackSame(123456789012345, $"long x = 123456789012345;", kernel, options);
        await SharedValueShouldBeReturnedBackSame(true, $"bool x = true;", kernel, options);
        await SharedValueShouldBeReturnedBackSame("hi!", $"var x = \"hi!\";", kernel, options);
        await SharedValueShouldBeReturnedBackSame("hi!", $"string x = \"hi!\";", kernel, options);
        await SharedValueShouldBeReturnedBackSame("«ταБЬℓσ»", $"string x = \"«ταБЬℓσ»\";", kernel, options);
        await SharedValueShouldBeReturnedBackSame(-123456.789, $"double x = -123456.789;", kernel, options);
        await SharedValueShouldBeReturnedBackSame(123456.789, $"double x = 123456.789;", kernel, options);

        options.SaveState();
    }

    [Theory]
    [JupyterHttpTestData(@"
data = [{""CategoryName"":""Road Frames"",""ProductName"":""HL Road Frame - Black, 58""},{""CategoryName"":""Road Frames"",""ProductName"":""HL Road Frame - Red, 58""},{""CategoryName"":""Helmets"",""ProductName"":""Sport-100 Helmet, Red""},{""CategoryName"":""Helmets"",""ProductName"":""Sport-100 Helmet, Black""}]
import pandas as pd
df = pd.DataFrame(data)", "df.equals(df_shared)", "True", KernelSpecName = PythonKernelName, AllowPlayback = false)]
    [JupyterHttpTestData(@"
data <- fromJSON('[{""CategoryName"":""Road Frames"",""ProductName"":""HL Road Frame - Black, 58""},{""CategoryName"":""Road Frames"",""ProductName"":""HL Road Frame - Red, 58""},{""CategoryName"":""Helmets"",""ProductName"":""Sport-100 Helmet, Red""},{""CategoryName"":""Helmets"",""ProductName"":""Sport-100 Helmet, Black""}]')
df <- data.frame(data)", "identical(df, df_shared)", "[1] TRUE", KernelSpecName = RKernelName, AllowPlayback = false)]
    public async Task can_share_dataframe_to_from_kernel(JupyterConnectionTestData connectionData, string createVarDF, string assertIdentical, string expectedAssertionResult)
    {
        var options = connectionData.GetConnectionOptions();

        var kernel = CreateCompositeKernelAsync(options);

        await kernel.SubmitCodeAsync(
            $"#!connect jupyter --kernel-name testKernel --kernel-spec {connectionData.KernelSpecName} {connectionData.GetConnectionString()}");
        
        var result = await kernel.SubmitCodeAsync($"#!testKernel\n{createVarDF}");
        var events = result.Events;

        events
            .Should()
            .NotContainErrors();

        var sentMessages = options.MessageTracker.SentMessages.ToSubscribedList();
        var recievedMessages = options.MessageTracker.ReceivedMessages.ToSubscribedList();

        result = await kernel.SubmitCodeAsync($"#!share --from testKernel df --as df_shared");
        events = result.Events;

        events
            .Should()
            .NotContainErrors();

        events
            .Should()
            .ContainSingle<ValueProduced>()
            .Which.Value.Should().BeAssignableTo<TabularDataResource>();

        result = await kernel.SubmitCodeAsync($"#!testKernel\n#!share --from csharp df_shared");
        events = result.Events;

        events
            .Should()
            .NotContainErrors();
        
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
}
