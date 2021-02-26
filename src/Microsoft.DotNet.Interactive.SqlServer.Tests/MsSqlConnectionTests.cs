// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.SqlServer.Tests
{
    public class MsSqlConnectionTests
    {
        private async Task<CompositeKernel> CreateKernel()
        {
            var csharpKernel = new CSharpKernel().UseNugetDirective();
            await csharpKernel.SubmitCodeAsync(@$"
#r ""nuget:microsoft.sqltoolsservice,3.0.0-release.53""
");

            // TODO: remove SQLKernel it is used to test current patch
            var kernel = new CompositeKernel
            {
                new SQLKernel(),
                csharpKernel,
                new KeyValueStoreKernel()
            };

            kernel.DefaultKernelName = csharpKernel.Name;

            kernel.UseKernelClientConnection(new MsSqlKernelConnection());

            return kernel;
        }

        [MsSqlFact]
        public async Task It_can_connect_and_query_data()
        {
            var connectionString = MsSqlFact.GetConnectionStringForTests();
            using var kernel = await CreateKernel();
            var result = await kernel.SubmitCodeAsync(
                             $"#!connect --kernel-name adventureworks mssql \"{connectionString}\"");

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
            var connectionString = MsSqlFact.GetConnectionStringForTests();
            using var kernel = await CreateKernel();
            var result = await kernel.SubmitCodeAsync(
                $"#!connect --kernel-name adventureworks mssql \"{connectionString}\"");

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
                    e.FormattedValues.Any(f => f.MimeType == "text/markdown"))
                .Which.FormattedValues.Single(f => f.MimeType == "text/markdown")
                .Value
                .Should()
                .Contain("- `#!sql-adventureworks`");

        }

        [MsSqlFact]
        public async Task It_can_scaffold_a_DbContext_in_a_CSharpKernel()
        {
            var connectionString = MsSqlFact.GetConnectionStringForTests();

            using var kernel = await CreateKernel();
            var result = await kernel.SubmitCodeAsync(
                             $"#!connect --kernel-name adventureworks mssql \"{connectionString}\" --create-dbcontext");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            result = await kernel.SubmitCodeAsync("adventureworks.AddressType.Count()");

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
            var connectionString = MsSqlFact.GetConnectionStringForTests();
            using var kernel = await CreateKernel();
            var result = await kernel.SubmitCodeAsync(
                             $"#!connect --kernel-name adventureworks mssql \"{connectionString}\"");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .NotContainErrors();

            result = await kernel.SubmitCodeAsync($@"
#!sql-adventureworks --mime-type {TabularDataFormatter.MimeType}
select * from sys.databases
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            var value = events.Should()
                    .ContainSingle<DisplayedValueProduced>(e =>
                        e.FormattedValues.Any(f => f.MimeType == HtmlFormatter.MimeType))
                              .Which;

            var tables = (IEnumerable<IEnumerable<IEnumerable<(string, object)>>>)value.Value;

            var table = tables.Single().ToTable();

            foreach (var row in table)
            {
                row["database_id"].Should().BeOfType<int>();
            }
        }

        [MsSqlFact]
        public async Task Empty_results_are_displayed_correctly()
        {
            var connectionString = MsSqlFact.GetConnectionStringForTests();
            using var kernel = await CreateKernel();
            var result = await kernel.SubmitCodeAsync(
                             $"#!connect --kernel-name adventureworks mssql \"{connectionString}\"");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .NotContainErrors();

            result = await kernel.SubmitCodeAsync($@"
#!sql-adventureworks --mime-type {TabularDataFormatter.MimeType}
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
    }
}