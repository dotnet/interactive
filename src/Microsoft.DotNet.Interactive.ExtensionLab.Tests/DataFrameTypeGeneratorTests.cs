// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Analysis;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests;

public class DataFrameTypeGeneratorTests
{
    [Fact]
    public async Task it_builds_a_type_based_on_a_DataFrame()
    {
        using var kernel = await CreateKernelAndGenerateType();

        kernel.TryGetValue("frame", out DataFrame newFrame)
            .Should()
            .BeTrue();

        newFrame.GetType().BaseType.Should().Be<DataFrame>();
        newFrame.GetType().Name.Should().Be("DataFrame_From_frame");
    }

    [Fact]
    public async Task Custom_DataFrame_contains_source_frame_data()
    {
        using var kernel = await CreateKernelAndGenerateType();

        kernel.TryGetValue("frame", out DataFrame newFrame)
            .Should()
            .BeTrue();

        newFrame.Rows.Count.Should().Be(3);
    }

    [Fact]
    public async Task completions_show_custom_DataFrame_members()
    {
        using var kernel = await CreateKernelAndGenerateType();

        var markupCode = "frame.Where(s => s.$$)";

        MarkupTestFile.GetPosition(markupCode, out var code, out var position);

        var events = await kernel.SendAsync(new RequestCompletions(code, new LinePosition(0, position.Value)));

        events
            .Events
            .Should()
            .ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Select(c => c.DisplayText)
            .Should()
            .Contain(new[] { "name", "is_available", "price_in_credits" });
    }

    [Fact]
    public void Custom_DataFrame_can_be_sliced_with_LINQ()
    {
        var frame = CreateDataFrame();

        var bananaFrame = new DerivedDataFrame(frame.Columns);

        bananaFrame.Count(row => row.is_available)
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task The_generated_source_code_can_be_displayed()
    {
        var kernel = new CSharpKernel()
            .UseNugetDirective();

        await DataFrameKernelExtension.LoadAsync(kernel);

        await kernel.SubmitCodeAsync($@"
#r ""{typeof(DataFrame).Assembly.Location}""
using Microsoft.Data.Analysis;
");
        var dataFrame = CreateDataFrame();

        await kernel.SendAsync(new SendValue("frame", dataFrame, FormattedValue.CreateSingleFromObject(dataFrame)));

        var result = await kernel.SubmitCodeAsync("#!linqify frame --show-code");

        result.Events
            .Should()
            .NotContainErrors();

        result.Events
            .Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle()
            .Which
            .Value
            .Should()
            .ContainAll(
                "public class DataFrame_", 
                "public System.String name => ");
    }

    [Fact]
    public async Task Completions_suggest_existing_DataFrame_variables()
    {
        var kernel = new CSharpKernel()
            .UseNugetDirective();

        await DataFrameKernelExtension.LoadAsync(kernel);

        await kernel.SubmitCodeAsync($@"
#r ""{typeof(DataFrame).Assembly.Location}""
using Microsoft.Data.Analysis;
");
        var dataFrameVariableName = "myDataFrame";
        var dataFrame = CreateDataFrame();

        await kernel.SendAsync(new SendValue(dataFrameVariableName, dataFrame, FormattedValue.CreateSingleFromObject(dataFrame)));

        var code = "#!linqify ";
        var result = await kernel.SendAsync(new RequestCompletions(code, new LinePosition(0, code.Length)));

        result.Events.Should().ContainSingle<CompletionsProduced>()
            .Which
            .Completions
            .Select(c => c.DisplayText)
            .Should()
            .Contain(dataFrameVariableName);
    }

    private static async Task<CSharpKernel> CreateKernelAndGenerateType(
        string magicCommand = "#!linqify frame")
    {
        var kernel = new CSharpKernel()
            .UseNugetDirective();

        await DataFrameKernelExtension.LoadAsync(kernel);

        await kernel.SubmitCodeAsync($@"
#r ""{typeof(DataFrame).Assembly.Location}""
using Microsoft.Data.Analysis;
");
        var dataFrame = CreateDataFrame();

        await kernel.SendAsync(new SendValue("frame", dataFrame, FormattedValue.CreateSingleFromObject(dataFrame)));

        await kernel.SubmitCodeAsync(magicCommand);

        return kernel;
    }

    private static DataFrame CreateDataFrame()
    {
        var names = new StringDataFrameColumn("name", 3);
        names[0] = "apple";
        names[1] = "pineapple";
        names[2] = "durian";

        var prices = new PrimitiveDataFrameColumn<decimal>("price in credits", 3);
        prices[0] = 12.2m;
        prices[1] = 22.12m;
        prices[2] = 3_000_000m;

        var availabilities = new BooleanDataFrameColumn("is_available", 3);
        availabilities[0] = false;
        availabilities[1] = false;
        availabilities[2] = true;

        return new DataFrame(
            names,
            prices,
            availabilities);
    }
}

public class DerivedDataFrame : DataFrame, IEnumerable<DerivedDataFrameRow>
{
    public DerivedDataFrame(IEnumerable<DataFrameColumn> columns)
        : base(columns)
    {
    }

    public IEnumerator<DerivedDataFrameRow> GetEnumerator() =>
        Rows.Select(row => new DerivedDataFrameRow(row)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() =>
        GetEnumerator();
}

public class DerivedDataFrameRow
{
    private readonly DataFrameRow _sourceRow;

    public DerivedDataFrameRow(DataFrameRow sourceRow)
    {
        _sourceRow = sourceRow;
    }

    public String name => (String) _sourceRow[0];
    public Decimal price => (Decimal) _sourceRow[1];
    public Boolean is_available => (Boolean) _sourceRow[2];
}