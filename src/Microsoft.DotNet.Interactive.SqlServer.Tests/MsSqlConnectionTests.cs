// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.Csv;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.SqlServer.Tests
{
    public class MsSqlConnectionTests : IDisposable
    {
        private async Task<CompositeKernel> CreateKernelAsync()
        {
            Formatter.SetPreferredMimeTypesFor(typeof(TabularDataResource), HtmlFormatter.MimeType, CsvFormatter.MimeType);
            var csharpKernel = new CSharpKernel().UseNugetDirective().UseValueSharing();
            await csharpKernel.SubmitCodeAsync(@"
#r ""nuget:microsoft.sqltoolsservice,3.0.0-release.163""
");

            // TODO: remove SQLKernel it is used to test current patch
            var kernel = new CompositeKernel
            {
                new SqlDiscoverabilityKernel(),
                csharpKernel,
                new KeyValueStoreKernel()
            };

            kernel.DefaultKernelName = csharpKernel.Name;

            var sqlKernelExtension = new MsSqlKernelExtension();
            await sqlKernelExtension.OnLoadAsync(kernel);

            return kernel;
        }

        [MsSqlFact]
        public async Task It_can_connect_and_query_data()
        {
            var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
            using var kernel = await CreateKernelAsync();
            var result = await kernel.SubmitCodeAsync(
                             $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .NotContainErrors();

            result = await kernel.SubmitCodeAsync(@"
#!sql-adventureworks
SELECT TOP 100 * FROM Person.Person
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                .NotContainErrors()
                .And
                .ContainSingle<DisplayedValueProduced>(e =>
                    e.FormattedValues.Any(f => f.MimeType == PlainTextFormatter.MimeType));

            events.Should()
                .ContainSingle<DisplayedValueProduced>(e =>
                    e.FormattedValues.Any(f => f.MimeType == HtmlFormatter.MimeType));
        }

        [MsSqlFact]
        public async Task sending_query_to_sql_will_generate_suggestions()
        {
            var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
            using var kernel = await CreateKernelAsync();
            var result = await kernel.SubmitCodeAsync(
                $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContainErrors();

            result = await kernel.SubmitCodeAsync(@"
#!sql
SELECT TOP 100 * FROM Person.Person
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                .NotContainErrors()
                .And
                .ContainSingle<DisplayedValueProduced>(e =>
                    e.FormattedValues.Any(f => f.MimeType == HtmlFormatter.MimeType))
                .Which.FormattedValues.Single(f => f.MimeType == HtmlFormatter.MimeType)
                .Value
                .Should()
                .Contain("#!sql-adventureworks")
                .And
                .Contain(" SELECT TOP * FROM");

        }

        [MsSqlFact]
        public async Task It_can_scaffold_a_DbContext_in_a_CSharpKernel()
        {
            var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();

            using var kernel = await CreateKernelAsync();
            var result = await kernel.SubmitCodeAsync(
                             $"#!connect mssql --kernel-name adventureworks \"{connectionString}\" --create-dbcontext");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            result = await kernel.SubmitCodeAsync("adventureworks.AddressTypes.Count()");

            events = result.KernelEvents.ToSubscribedList();
            events.Should().NotContainErrors();

            events.Should()
                  .ContainSingle<ReturnValueProduced>()
                  .Which
                  .Value
                  .As<int>()
                  .Should()
                  .Be(6);
        }

        [MsSqlFact]
        public async Task Field_types_are_deserialized_correctly()
        {
            var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
            using var kernel = await CreateKernelAsync();
            var result = await kernel.SubmitCodeAsync(
                             $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .NotContainErrors();

            result = await kernel.SubmitCodeAsync($@"
#!sql-adventureworks
select * from sys.databases
");

            var events = result.KernelEvents.ToSubscribedList();

            events.ShouldDisplayTabularDataResourceWhich()
                  .Schema
                  .Fields
                  .Should()
                  .ContainSingle(f => f.Name == "database_id")
                  .Which
                  .Type
                  .Should()
                  .Be(TableSchemaFieldType.Integer);
        }

        [MsSqlFact]
        public async Task query_produces_expected_formatted_values()
        {
            var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
            using var kernel = await CreateKernelAsync();
            var result = await kernel.SubmitCodeAsync(
                $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContainErrors();

            result = await kernel.SubmitCodeAsync($@"
#!sql-adventureworks
select * from sys.databases
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            events.Should()
                .ContainSingle<DisplayedValueProduced>(fvp => fvp.Value is DataExplorer<TabularDataResource>)
                .Which
                .FormattedValues.Select(fv => fv.MimeType)
                .Should()
                .BeEquivalentTo(HtmlFormatter.MimeType, CsvFormatter.MimeType);
        }

        [MsSqlFact]
        public async Task Empty_results_are_displayed_correctly()
        {
            var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
            using var kernel = await CreateKernelAsync();
            var result = await kernel.SubmitCodeAsync(
                             $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .NotContainErrors();

            result = await kernel.SubmitCodeAsync($@"
#!sql-adventureworks
use tempdb;
create table dbo.EmptyTable(column1 int, column2 int, column3 int);
select * from dbo.EmptyTable;
drop table dbo.EmptyTable;
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should()
                .NotContainErrors()
                .And
                    .ContainSingle<DisplayedValueProduced>(e =>
                        e.FormattedValues.Any(f => f.MimeType == PlainTextFormatter.MimeType && f.Value.ToString().StartsWith("Info")));
        }

        [MsSqlFact]
        public async Task It_can_store_result_set_with_a_name()
        {
            var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
            using var kernel = await CreateKernelAsync();
            await kernel.SubmitCodeAsync(
                             $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

            // Run query with result set
            await kernel.SubmitCodeAsync($@"
#!sql-adventureworks --name my_data_result
select * from sys.databases
");

            // Use share to fetch result set
            var csharpResults = await kernel.SubmitCodeAsync($@"
#!csharp
#!share --from sql-adventureworks my_data_result
my_data_result");

            // Verify the variable loaded is of the correct type and has the expected number of result sets
            var csharpEvents = csharpResults.KernelEvents.ToSubscribedList();
            csharpEvents
                .Should()
                .ContainSingle<ReturnValueProduced>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<TabularDataResource>>()
                .Which.Count()
                .Should()
                .Be(1);
        }

        [MsSqlFact]
        public async Task It_can_store_multiple_result_set_with_a_name()
        {
            var connectionString = MsSqlFactAttribute.GetConnectionStringForTests();
            using var kernel = await CreateKernelAsync();
            await kernel.SubmitCodeAsync(
                $"#!connect mssql --kernel-name adventureworks \"{connectionString}\"");

            // Run query with result set
            await kernel.SubmitCodeAsync($@"
#!sql-adventureworks --name my_data_result
select * from sys.databases
select * from sys.databases
");

            // Use share to fetch result set
            var csharpResults = await kernel.SubmitCodeAsync($@"
#!csharp
#!share --from sql-adventureworks my_data_result
my_data_result");

            // Verify the variable loaded is of the correct type and has the expected number of result sets
            var csharpEvents = csharpResults.KernelEvents.ToSubscribedList();
            csharpEvents
                .Should()
                .ContainSingle<ReturnValueProduced>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<IEnumerable<TabularDataResource>>()
                .Which.Count()
                .Should()
                .Be(2);
        }

        public static readonly IEnumerable<object[]> SharedObjectVariables =
            new List<object[]>
            {
                new object[] { "Guid testVar = Guid.Parse(\"4df65065-2369-4d63-a6b0-20dc7cdd02fe\");", Guid.Parse("4df65065-2369-4d63-a6b0-20dc7cdd02fe") } // GUID
            };

        [MsSqlTheory]
        [InlineData("var testVar = 2;", 2)] // var
        [InlineData("string testVar = \"hi!\";", "hi!")] // string
        [InlineData("string testVar = \"tricky'string\";", "tricky'string")] // string with '
        [InlineData("string testVar = \"«ταБЬℓσ»\";", "«ταБЬℓσ»")] // unicode
        [InlineData("string testVar = \"\";", "")] // Empty string
        [InlineData("double testVar = 123456.789;", 123456.789)] // double
        [InlineData("decimal testVar = 123456.789M;", 123456.789, typeof(Decimal))] // decimal
        [InlineData("bool testVar = false;", false)] // bool
        [InlineData("char testVar = 'a';", "a")] // char
        [InlineData("char testVar = '\\'';", "'")] // ' char
        [InlineData("byte testVar = 123;", (byte)123)] // byte
        [InlineData("int testVar = 123456;", 123456)] // int
        [InlineData("long testVar = 123456789012345;", 123456789012345)] // long
        [InlineData("short testVar = 123;", (short)123)] // short
        [MemberData(nameof(SharedObjectVariables))]
        public async Task Shared_variable_can_be_used_to_parameterize_a_sql_query(string csharpVariableDeclaration, object expectedValue, Type changeType = null)
        {
            using var kernel = await CreateKernelAsync();
            var result = await kernel.SubmitCodeAsync(
                             $"#!connect mssql --kernel-name adventureworks \"{MsSqlFactAttribute.GetConnectionStringForTests()}\"");

            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContainErrors();

            await kernel.SendAsync(new SubmitCode(csharpVariableDeclaration));

            var code = @"
#!sql-adventureworks
#!share --from csharp testVar
select @testVar";

            result = await kernel.SendAsync(new SubmitCode(code));

            var events = result.KernelEvents.ToSubscribedList();

            var data = events.ShouldDisplayTabularDataResourceWhich();

            if (changeType != null)
            {
                // Decimals can't be made constants so need to convert at runtime
                expectedValue = Convert.ChangeType(expectedValue, changeType);
            }
            data.Data
                .Should()
                .ContainSingle()
                .Which
                .Should()
                .ContainValue(expectedValue);
        }

        [MsSqlFact]
        public async Task Multiple_shared_variable_can_be_used_to_parameterize_a_sql_query()
        {
            using var kernel = await CreateKernelAsync();
            var result = await kernel.SubmitCodeAsync(
                             $"#!connect mssql --kernel-name adventureworks \"{MsSqlFactAttribute.GetConnectionStringForTests()}\"");

            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContainErrors();

            var csharpCode = "string x = \"Hello world!\";";
            await kernel.SendAsync(new SubmitCode(csharpCode));

            csharpCode = "int y = 123;";
            await kernel.SendAsync(new SubmitCode(csharpCode));

            var code = @"
#!sql-adventureworks
#!share --from csharp x
#!share --from csharp y
select @x, @y";

            result = await kernel.SendAsync(new SubmitCode(code));

            var events = result.KernelEvents.ToSubscribedList();

            events.ShouldDisplayTabularDataResourceWhich().Data
                  .Should()
                  .ContainSingle()
                  .Which
                  .Should()
                  .ContainValues(new object[] { "Hello world!", 123 });
        }
        
        public void Dispose()
        {
            DataExplorer.ResetToDefault();
            Formatter.ResetToDefault();
        }
    }
}