// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using System.Linq;
using System;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.App;
using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.PostgreSql.Tests;

[Trait("Databases", "Data query tests")]
public class PostgreSqlConnectionTests : IDisposable
{
    private static CompositeKernel CreateKernel()
    {
        var csharpKernel = new CSharpKernel().UseNugetDirective().UseValueSharing();

        var kernel = new CompositeKernel
        {
            csharpKernel,
            new KeyValueStoreKernel()
        };

        kernel.DefaultKernelName = csharpKernel.Name;

        PostgreSqlKernelExtension.Load(kernel);

        return kernel;
    }


    [PostgreSqlFact]
    public async Task It_can_connect_and_query_data()
    {
        var connectionString = PostgreSqlFactAttribute.GetConnectionStringForTests();
        using var kernel = CreateKernel();
        var connect = $"#!connect postgres --kernel-name adventureworks \"{connectionString}\"";
        var result = await kernel.SubmitCodeAsync(connect);
        result.Events.Should().NotContainErrors();

        result = await kernel.SubmitCodeAsync("""
            #!sql-adventureworks
            SELECT * FROM customers LIMIT 100;
            """);

        result.Events.Should().NotContainErrors();
        result.Events.Should()
            .ContainSingle<DisplayedValueProduced>(fvp => fvp.Value is DataExplorer<TabularDataResource>)
            .Which
            .FormattedValues.Select(fv => fv.MimeType)
            .Should()
            .BeEquivalentTo(HtmlFormatter.MimeType, CsvFormatter.MimeType);
    }

    [PostgreSqlFact]
    public async Task It_returns_error_if_query_is_not_valid()
    {
        var connectionString = PostgreSqlFactAttribute.GetConnectionStringForTests();
        using var kernel = CreateKernel();
        var connect = $"#!connect postgres --kernel-name adventureworks \"{connectionString}\"";
        var result = await kernel.SubmitCodeAsync(connect);
        result.Events.Should().NotContainErrors();

        result = await kernel.SubmitCodeAsync("""
            #!sql-adventureworks
            SELECT not_known_column FROM customers LIMIT 100;
            """);

        result.Events.Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Contain("column \"not_known_column\" does not exist");
    }

//     public async Task When_variable_does_not_exist_then_an_error_is_returned()
// {
//     var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
//     using var kernel = await CreateKernelAsync();
//     var result = await kernel.SubmitCodeAsync(
//         $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

//     result.Events
//         .Should()
//         .NotContainErrors();

//     var sqlKernel = kernel.FindKernelByName("sql-adventureworks");

//     result = await sqlKernel.SendAsync(new RequestValue("my_data_result"));

//     result.Events.Should()
//           .ContainSingle<CommandFailed>()
//           .Which
//           .Message
//           .Should()
//           .Contain("Value 'my_data_result' not found in kernel sql-adventureworks");
// }

//  [MsSqlFact]
//     public async Task It_can_store_result_set_with_a_name()
//     {
//         var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
//         using var kernel = await CreateKernelAsync();
//         await kernel.SubmitCodeAsync(
//             $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

//         // Run query with result set
//         await kernel.SubmitCodeAsync($@"
// #!sql-adventureworks --name my_data_result
// select * from sys.databases
// ");

//         // Use share to fetch result set
//         var result = await kernel.SubmitCodeAsync($@"
// #!csharp
// #!share --from sql-adventureworks my_data_result
// my_data_result");

//         // Verify the variable loaded is of the correct type and has the expected number of result sets
//         result.Events
//               .Should()
//               .ContainSingle<ReturnValueProduced>()
//               .Which
//               .Value
//               .Should()
//               .BeAssignableTo<IEnumerable<TabularDataResource>>()
//               .Which.Count()
//               .Should()
//               .Be(1);
//     }

//     [MsSqlFact]
//     public async Task Stored_query_results_are_listed_in_ValueInfos()
//     {
//         var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
//         using var kernel = await CreateKernelAsync();
//         await kernel.SubmitCodeAsync(
//             $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

//         // Run query with result set
//         await kernel.SubmitCodeAsync($@"
// #!sql-adventureworks --name my_data_result
// select * from sys.databases
// ");

//         var sqlKernel = kernel.FindKernelByName("sql-adventureworks");

//         var result = await sqlKernel.SendAsync(new RequestValueInfos());

//         var valueInfos = result.Events.Should().ContainSingle<ValueInfosProduced>()
//             .Which.ValueInfos;

//         valueInfos.Should().Contain(v => v.Name == "my_data_result");
//     }

//     [MsSqlFact]
//     public async Task Storing_results_does_interfere_with_subsequent_executions()
//     {
//         var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
//         using var kernel = await CreateKernelAsync();
//         await kernel.SubmitCodeAsync(
//             $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

//         // Run query with result set
//         await kernel.SubmitCodeAsync($@"
// #!sql-adventureworks --name my_data_result
// select * from sys.databases
// ");

//         var sqlKernel = kernel.FindKernelByName("sql-adventureworks");

//         var result = await sqlKernel.SendAsync(new RequestValueInfos());

//         var valueInfos = result.Events.Should().ContainSingle<ValueInfosProduced>()
//             .Which.ValueInfos;

//         valueInfos.Should().Contain(v => v.Name == "my_data_result");

//          result =  await kernel.SubmitCodeAsync($@"
// #!sql-adventureworks --name my_data_result
// select * from sys.databases
// ");

//          result.Events.Should().NotContainErrors();
//     }

// [MsSqlFact]
//     public async Task It_can_store_multiple_result_set_with_a_name()
//     {
//         var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
//         using var kernel = await CreateKernelAsync();
//         await kernel.SubmitCodeAsync(
//             $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

//         // Run query with result set
//         await kernel.SubmitCodeAsync($@"
// #!sql-adventureworks --name my_data_result
// select * from sys.databases
// select * from sys.databases
// ");

//         // Use share to fetch result set
//         var result = await kernel.SubmitCodeAsync($@"
// #!csharp
// #!share --from sql-adventureworks my_data_result
// my_data_result");

//         // Verify the variable loaded is of the correct type and has the expected number of result sets
//         result.Events
//               .Should()
//               .ContainSingle<ReturnValueProduced>()
//               .Which
//               .Value
//               .Should()
//               .BeAssignableTo<IEnumerable<TabularDataResource>>()
//               .Which.Count()
//               .Should()
//               .Be(2);
//     }



    public void Dispose()
    {
        DataExplorer.ResetToDefault();
    }
}