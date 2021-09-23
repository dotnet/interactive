﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.ExtensionLab;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.SqlServer.Tests
{
    public class MsSqlConnectionTests : IDisposable
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
           
            kernel.UseKernelClientConnection(new ConnectMsSqlCommand());
            kernel.UseNteractDataExplorer();
            kernel.UseSandDanceExplorer();

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
#!sql-adventureworks --mime-type {TabularDataResourceFormatter.MimeType}
select * from sys.databases
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            var value = events.Should()
                    .ContainSingle<DisplayedValueProduced>(e =>
                        e.FormattedValues.Any(f => f.MimeType == HtmlFormatter.MimeType))
                              .Which;

            var table = (NteractDataExplorer) value.Value;

            table.Data
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
#!sql-adventureworks --mime-type {TabularDataResourceFormatter.MimeType}
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

        public void Dispose()
        {
            Formatter.ResetToDefault();
        }
    }
}