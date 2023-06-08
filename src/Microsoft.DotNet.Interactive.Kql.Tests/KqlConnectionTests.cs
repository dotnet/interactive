// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.Diagnostics.Runtime.Utilities;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.SqlServer;
using Microsoft.DotNet.Interactive.Tests.Utility;

using Xunit;

namespace Microsoft.DotNet.Interactive.Kql.Tests;

public class KqlConnectionTests : IDisposable
{
    private static async Task<CompositeKernel> CreateKernelAsync()
    {
        Formatter.SetPreferredMimeTypesFor(typeof(TabularDataResource), HtmlFormatter.MimeType, CsvFormatter.MimeType);
        var csharpKernel = new CSharpKernel().UseNugetDirective();

        var kernel = new CompositeKernel
        {
            new KqlDiscoverabilityKernel(),
            csharpKernel,
            new KeyValueStoreKernel()
        };

        kernel.DefaultKernelName = csharpKernel.Name;

        var kqlKernelExtension = new KqlKernelExtension();
        await kqlKernelExtension.OnLoadAsync(kernel);

        return kernel;
    }

    [KqlFact]
    public async Task It_can_connect_and_query_data()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
            .Should()
            .NotContainErrors();

        result = await kernel.SubmitCodeAsync(@"
#!kql-KustoHelp
StormEvents | take 10
            ");

        result.Events.Should()
              .NotContainErrors()
              .And
              .ContainSingle<DisplayedValueProduced>(e =>
                                                         e.FormattedValues.Any(f => f.MimeType == PlainTextFormatter.MimeType));

        result.Events.Should()
              .ContainSingle<DisplayedValueProduced>(e =>
                                                         e.FormattedValues.Any(f => f.MimeType == HtmlFormatter.MimeType));
    }

    [KqlFact]
    public async Task It_does_not_add_a_kernel_on_connection_failure()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync(
            "#!connect kql --kernel-name KustoHelp --cluster \"invalid_cluster\" --database \"Samples\"");

        result.Events
            .Should()
            .ContainSingle<CommandFailed>();

        var kqlKernel = kernel.FindKernelByName("kql-KustoHelp");

        kqlKernel.Should().BeNull();
    }

    [KqlFact]
    public async Task It_allows_to_retry_connecting()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync(
            "#!connect kql --kernel-name KustoHelp --cluster \"invalid_cluster\" --database \"Samples\"");

        result.Events
            .Should()
            .ContainSingle<CommandFailed>();

        result = await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
            .Should()
            .NotContainErrors();
    }

    [KqlFact]
    public async Task It_gives_error_if_kernel_name_is_already_used()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync($"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
            .Should()
            .NotContainErrors();

        result = await kernel.SubmitCodeAsync($"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Contain("A kernel with name KustoHelp is already present. Use a different value for the --kernel-name option.");
    }

    [KqlFact]
    public async Task It_can_store_result_set_with_a_name()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
            .Should()
            .NotContainErrors();

        result = await kernel.SubmitCodeAsync(@"
#!kql-KustoHelp --name my_data_result
StormEvents | take 10
            ");

        var kqlKernel = kernel.FindKernelByName("kql-KustoHelp");

        result = await kqlKernel.SendAsync(new RequestValue("my_data_result"));

        result.Events.Should().ContainSingle<ValueProduced>()
              .Which.Value.Should().BeAssignableTo<IEnumerable<TabularDataResource>>();
    }

    [KqlFact]
    public async Task Storing_results_does_interfere_with_subsequent_executions()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        await kernel.SubmitCodeAsync(
             $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        await kernel.SubmitCodeAsync(@"
#!kql-KustoHelp --name my_data_result
StormEvents | take 10
            ");

        var kqlKernel = kernel.FindKernelByName("kql-KustoHelp");

        var result = await kqlKernel.SendAsync(new RequestValue("my_data_result"));

        result.Events.Should().ContainSingle<ValueProduced>()
            .Which.Value.Should().BeAssignableTo<IEnumerable<TabularDataResource>>();

        await kernel.SubmitCodeAsync(@"
#!kql-KustoHelp
StormEvents | take 11
            ");

        result.Events
            .Should()
            .NotContainErrors();
    }

    [KqlFact]
    public async Task Stored_query_results_are_listed_in_ValueInfos()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
            .Should()
            .NotContainErrors();

        result = await kernel.SubmitCodeAsync(@"
#!kql-KustoHelp --name my_data_result
StormEvents | take 10
            ");

        var kqlKernel = kernel.FindKernelByName("kql-KustoHelp");

        result = await kqlKernel.SendAsync(new RequestValueInfos());

        var valueInfos = result.Events.Should().ContainSingle<ValueInfosProduced>()
            .Which.ValueInfos;

        valueInfos.Should().Contain(v => v.Name == "my_data_result");
    }

    [KqlFact]
    public async Task When_variable_does_not_exist_then_an_error_is_returned()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");


        result.Events
            .Should()
            .NotContainErrors();

        var kqlKernel = kernel.FindKernelByName("kql-KustoHelp");

        result = await kqlKernel.SendAsync(new RequestValue("my_data_result"));

        result.Events.Should()
              .ContainSingle<CommandFailed>()
              .Which
              .Message
              .Should()
              .Contain("Value 'my_data_result' not found in kernel kql-KustoHelp");
    }

    [KqlFact]
    public async Task sending_query_to_kusto_will_generate_suggestions()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
              .Should()
              .NotContainErrors();

        var queryCode = "StormEvents | take 10";
        result = await kernel.SubmitCodeAsync($@"
#!kql
{queryCode}
");

        result.Events
              .Should()
              .ContainSingle<DisplayedValueProduced>(e =>
                                                         e.FormattedValues.Any(f => f.MimeType == HtmlFormatter.MimeType))
              .Which.FormattedValues.Single(f => f.MimeType == HtmlFormatter.MimeType)
              .Value
              .Should()
              .Contain("#!kql-KustoHelp")
              .And
              .Contain(queryCode);
    }

    [KqlFact]
    public async Task Field_types_are_deserialized_correctly()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
            .Should()
            .NotContainErrors();

        result = await kernel.SubmitCodeAsync($@"
#!kql-KustoHelp
StormEvents | take 10
");

        result.Events.ShouldDisplayTabularDataResourceWhich()
              .Schema
              .Fields
              .Should()
              .ContainSingle(f => f.Name == "StartTime")
              .Which
              .Type
              .Should()
              .Be(TableSchemaFieldType.DateTime);
    }

    [KqlFact]
    public async Task query_produces_expected_formatted_values()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
            .Should()
            .NotContainErrors();

        result = await kernel.SubmitCodeAsync($@"
#!kql-KustoHelp
StormEvents | take 10
");

        result.Events.Should().NotContainErrors();

        result.Events.Should()
              .ContainSingle<DisplayedValueProduced>(fvp => fvp.Value is DataExplorer<TabularDataResource>)
              .Which
              .FormattedValues.Select(fv => fv.MimeType)
              .Should()
              .BeEquivalentTo(HtmlFormatter.MimeType, CsvFormatter.MimeType);
    }

    [KqlFact]
    public async Task Empty_results_are_displayed_correctly()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
              .Should()
              .NotContainErrors();

        result = await kernel.SubmitCodeAsync($@"
#!kql-KustoHelp
StormEvents | take 0
");

        result.Events
              .Should()
              .NotContainErrors()
              .And
              .ContainSingle<DisplayedValueProduced>(e =>
                                                         e.FormattedValues.Any(f => f.MimeType == PlainTextFormatter.MimeType && f.Value.ToString().StartsWith("Info")));
    }

    [KqlTheory]
    [InlineData("var testVar = 2;", (long)2)] // var
    [InlineData("var testVar = \"hi!\";", "hi!")] // var string
    [InlineData("string testVar = \"hi!\";", "hi!")] // string
    [InlineData("string testVar = \"«ταБЬℓσ»\";", "«ταБЬℓσ»")] // unicode
    [InlineData("string testVar = \"\";", "")] // Empty string
    [InlineData("double testVar = 123456.789;", 123456.789)] // double
    [InlineData("decimal testVar = 123456.789M;", 123456.789)] // decimal
    [InlineData("bool testVar = false;", (sbyte)0)] // bool
    [InlineData("char testVar = 'a';", "a")] // char
    [InlineData("char testVar = '\\'';", "'")] // ' char
    [InlineData("byte testVar = 123;", (long)123)] // byte
    [InlineData("int testVar = 123456;", (long)123456)] // int
    [InlineData("long testVar = 123456789012345;", 123456789012345)] // long
    [InlineData("short testVar = 123;", (long)123)] // short
    [InlineData("sbyte testVar = 123;", (long)123)] // sbyte
    [InlineData("uint testVar = 123456;", (long)123456)] // uint
    [InlineData("ulong testVar = 123456789012345;", 123456789012345)] // ulong
    [InlineData("ushort testVar = 123;", (long)123)] // ushort
    public async Task Shared_variable_can_be_used_to_parameterize_a_kql_query(string csharpVariableDeclaration, object expectedValue)
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        var result = await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
            .Should()
            .NotContainErrors();

        await kernel.SendAsync(new SubmitCode(csharpVariableDeclaration));

        var code = @"
#!kql-KustoHelp
#!share --from csharp testVar
print testVar";

        result = await kernel.SendAsync(new SubmitCode(code));

        result.Events
              .ShouldDisplayTabularDataResourceWhich()
              .Data
              .Should()
              .ContainSingle()
              .Which
              .Should()
              .ContainValue(expectedValue);
    }

    [KqlTheory]
    [InlineData("string testVar = null;")] // Don't support null vars currently
    [InlineData("nint testVar = 123456;")] // Unsupported type
    [InlineData("nuint testVar = 123456;")] // Unsupported type
    [InlineData("var testVar = new List<int>();")] // Unsupported type
    [InlineData("string testVar = \"tricky\\\"string\";")] // string with ", bug https://github.com/microsoft/sqltoolsservice/issues/1271
    public async Task Invalid_shared_variables_are_handled_correctly(string csharpVariableDeclaration)
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();

        var result = await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        result.Events
            .Should()
            .NotContainErrors();

        await kernel.SendAsync(new SubmitCode(csharpVariableDeclaration));

        var code = @"
#!kql-KustoHelp
#!share --from csharp testVar
print testVar";

        result = await kernel.SendAsync(new SubmitCode(code));

        result.Events.Should().ContainSingle<CommandFailed>();
    }

    [KqlFact]
    public async Task Shared_variable_are_not_stored_as_part_of_the_resultSet()
    {
        var cluster = KqlFactAttribute.GetClusterForTests();
        using var kernel = await CreateKernelAsync();
        await kernel.SubmitCodeAsync(
            $"#!connect kql --kernel-name KustoHelp --cluster \"{cluster}\" --database \"Samples\"");

        await kernel.SendAsync(new SubmitCode(@"var testVar = 2;"));

        var code = @"
#!kql-KustoHelp --name testQuery
#!share --from csharp testVar
StormEvents | take testVar";

        await kernel.SendAsync(new SubmitCode(code));

        var kustoKernel = kernel.FindKernelByName("kql-KustoHelp") as ToolsServiceKernel;

        kustoKernel.TryGetValue<IEnumerable<object>>("testQuery", out var resultSet);

        resultSet.Should().NotBeNull().And.HaveCount(1);
    }

    public void Dispose()
    {
        Formatter.ResetToDefault();
        DataExplorer.ResetToDefault();
    }
}