// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions.Execution;

using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Text.Json;
using Assent;
using FluentAssertions;
using Microsoft.Data.Analysis;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Xunit;

namespace Microsoft.DotNet.Interactive.Data.Tests;

public class DataFrameTests : IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private readonly Configuration _configuration;

    public DataFrameTests()
    {
        _configuration = new Configuration()
            .SetInteractive(Debugger.IsAttached)
            .UsingExtension("json");
        
        DataFrameKernelExtension.RegisterDataFrameFormatters();
        _disposables.Add(Disposable.Create( Formatter.ResetToDefault));
        _disposables.Add(new AssertionScope());
    }
    [Fact]
    public void can_create_a_dataFrame_from_tabularDataResource()
    {
        var tabularData =JsonDocument.Parse(@"{
            ""schema"": {
                ""fields"": [
                    {
                        ""name"": ""Name"",
                        ""type"": ""string""
                    },
                    {
                        ""name"": ""IsValid"",
                        ""type"": ""boolean""
                    },
                    {
                        ""name"": ""Cost"",
                        ""type"": ""number""
                    }
                ]
            },
            ""data"": [
                {
                    ""Name""    : ""Q"",
                    ""IsValid"" : false,
                    ""Cost""    : 10.0
                },
                {
                    ""Name""    : ""U"",
                    ""IsValid"" :false,
                    ""Cost""    : 5.0
                },
                {
                    ""Name""    : ""E"",
                    ""IsValid"" :true,
                    ""Cost""    : 10.2
                },
                {
                    ""Name""    : ""S"",
                    ""IsValid"" :false,
                    ""Cost""    : 10.0
                },
                {
                    ""Name""    : ""T"",
                    ""IsValid"" :false,
                    ""Cost""    : 10.0
                }
            ]
        }").RootElement.Deserialize<TabularDataResource>();

        var dataFrame = tabularData.ToDataFrame();

        dataFrame.Columns.Count.Should().Be(3);
        dataFrame.Rows.Count.Should().Be(5);
        dataFrame.Columns[0].Name.Should().Be("Name");
        dataFrame.Rows[1][0].Should().Be("U");
    }

    [Fact]
    public void can_format_to_json()
    {
        var columns = new DataFrameColumn[]
        {
            new Int32DataFrameColumn("Int32Column", new[] {1, 2, 3}),
            new StringDataFrameColumn("StringColumn", new[] {"a", "b", "c"})
        };
        var dataFrame = new DataFrame(columns);
        var json = dataFrame.ToDisplayString("application/json");
        this.Assent(json, _configuration);
    }

    [Fact]
    public void the_json_representation_is_a_tabularDataResource()
    {
        var columns = new DataFrameColumn[]
        {
            new Int32DataFrameColumn("Int32Column", new[] {1, 2, 3}),
            new StringDataFrameColumn("StringColumn", new[] {"a", "b", "c"})
        };
        var dataFrame = new DataFrame(columns);
        var json = JsonDocument.Parse( dataFrame.ToDisplayString("application/json")).RootElement;

        json.GetProperty("schema").GetProperty("fields").EnumerateArray().Should().HaveCount(2);
        json.GetProperty("data").EnumerateArray().Should().HaveCount(3);

    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}