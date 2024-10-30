// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DuckDB.NET.Data;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.DuckDB.Tests;

public class DuckDbConnectionTests
{

    [Fact]
    public async Task It_can_connect_and_query_data()
    {
        using var kernel = new CompositeKernel();
        kernel.Add(new CSharpKernel());
        kernel.Add(new KeyValueStoreKernel());

        kernel.AddConnectDirective(new ConnectDuckDBDirective());

        using var _ = CreateInMemoryDuckDB(out var connectionString);

        var result = await kernel.SubmitCodeAsync(
            $"#!connect duckdb --kernel-name mydb  \"{connectionString}\"");

        result.Events
            .Should()
            .NotContainErrors();

        result = await kernel.SubmitCodeAsync(@"
#!mydb
SELECT SUM(deliciousness) FROM fruit GROUP BY color
");

        result.Events.Should().NotContainErrors();

        result.Events.Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which
            .FormattedValues
            .Should()
            .ContainSingle(f => f.MimeType == HtmlFormatter.MimeType);
    }

    [Fact]
    public async Task It_can_create_collections()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel(),
            new KeyValueStoreKernel()
        };

        kernel.AddConnectDirective(new ConnectDuckDBDirective());

        using var _ = CreateInMemoryDuckDB(out var connectionString);

        var result = await kernel.SubmitCodeAsync(
            $"#!connect duckdb --kernel-name mydb  \"{connectionString}\"");

        result.Events
            .Should()
            .NotContainErrors();

        result = await kernel.SubmitCodeAsync(@"
#!mydb
CREATE TABLE defaultVectorCollection(
 id TEXT,
payload TEXT,
embedding FLOAT[],
tags TEXT[],
timestamp TEXT,
PRIMARY KEY(id));
");
        result.Events.Should().NotContainErrors();

        result = await kernel.SubmitCodeAsync(@"
#!mydb
SHOW ALL TABLES;
");

        result.Events.Should().NotContainErrors();

        var displayValueProduced = result.Events.Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which;

        displayValueProduced.FormattedValues
            .Should()
            .ContainSingle(f => f.MimeType == HtmlFormatter.MimeType);
        var res = displayValueProduced.Value as DataExplorer<TabularDataResource>;

        var row = res.Data.Data.First(e => e.Any(e => e.Key == "name" && ((string)e.Value) == "defaultVectorCollection"));

        var types = row.First(e => e.Key == "column_types").Value as List<string>;

        types.Should().BeEquivalentTo(new[] { "VARCHAR", "VARCHAR", "FLOAT[]", "VARCHAR[]", "VARCHAR" });

    }

    [Fact]
    public async Task It_can_connect_and_query_data_with_list_type()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel(),
            new KeyValueStoreKernel()
        };

        kernel.AddConnectDirective(new ConnectDuckDBDirective());

        using var _ = CreateInMemoryDuckDB(out var connectionString);

        var result = await kernel.SubmitCodeAsync(
            $"#!connect duckdb --kernel-name mydb  \"{connectionString}\"");

        result.Events
            .Should()
            .NotContainErrors();

        result = await kernel.SubmitCodeAsync(@"
#!mydb
SHOW ALL TABLES;
");
        result.Events.Should().NotContainErrors();

        result = await kernel.SubmitCodeAsync(@"
#!mydb
SELECT * FROM fruit 
");

        result.Events.Should().NotContainErrors();

        var displayValueProduced = result.Events.Should()
            .ContainSingle<DisplayedValueProduced>()
            .Which;

        displayValueProduced.FormattedValues
            .Should()
            .ContainSingle(f => f.MimeType == HtmlFormatter.MimeType);
        var res = displayValueProduced.Value as DataExplorer<TabularDataResource>;

        res.Data.Data.First().Last().Value.Should().BeEquivalentTo(new[] { "a", "b", "c" });
    }

    [Fact]
    public async Task It_can_handle_multiple_statements()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel(),
            new KeyValueStoreKernel()
        };

        kernel.AddConnectDirective(new ConnectDuckDBDirective());

        using var _ = CreateInMemoryDuckDB(out var connectionString);

        var result = await kernel.SubmitCodeAsync(
            $"#!connect duckdb --kernel-name mydb --connection-string \"{connectionString}\"");

        result.Events
            .Should()
            .NotContainErrors();

        result = await kernel.SubmitCodeAsync(@"
#!mydb
SELECT SUM(deliciousness) FROM fruit GROUP BY color;

SELECT SUM(deliciousness) FROM fruit GROUP BY name;
");

        result.Events.Should().NotContainErrors();

        result.Events.OfType<DisplayedValueProduced>().Should().HaveCount(2);
    }

    [Fact]
    public async Task It_can_store_result_set_with_a_name()
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel(),
            new KeyValueStoreKernel()
        };

        kernel.AddConnectDirective(new ConnectDuckDBDirective());

        using var _ = CreateInMemoryDuckDB(out var connectionString);

        var result = await kernel.SubmitCodeAsync(
            $"#!connect duckdb --kernel-name mydb --connection-string \"{connectionString}\"");

        result.Events
            .Should()
            .NotContainErrors();

        result = await kernel.SubmitCodeAsync(@"
#!mydb --name my_data_result
SELECT SUM(deliciousness) FROM fruit GROUP BY color
");

        var duckDbKernel = kernel.FindKernelByName("mydb");

        result = await duckDbKernel.SendAsync(new RequestValue("my_data_result"));

        result.Events.Should().ContainSingle<ValueProduced>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<TabularDataResource>>();
    }

    internal static IDisposable CreateInMemoryDuckDB(out string connectionString)
    {
        connectionString = $"Data Source={DuckDBConnectionStringBuilder.InMemorySharedDataSource}";
        var topLevelConnection = new DuckDBConnection(connectionString);
        topLevelConnection.Open();
        
        var createCommand = topLevelConnection.CreateCommand();
        createCommand.CommandText =
            @"
CREATE TABLE fruit (
    name TEXT,
    color TEXT,
    deliciousness INTEGER,
    numbers FLOAT[],
    labels TEXT[]
);
            ";
        createCommand.ExecuteNonQuery();

        var updateCommand = topLevelConnection.CreateCommand();
        updateCommand.CommandText =
            "INSERT INTO fruit VALUES ('apple', 'green', 10, [1,2.3],['a', 'b', 'c']), ('banana', 'yellow', 11, [1,2.3], ['a', 'b', 'c']), ('banana', 'green', 11, [1,2.3],['a', 'b', 'c'])";
        updateCommand.ExecuteReader();

        return topLevelConnection;
    }
}