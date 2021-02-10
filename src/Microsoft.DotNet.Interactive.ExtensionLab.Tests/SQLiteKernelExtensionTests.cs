// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Assent;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;
using Enumerable = System.Linq.Enumerable;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public class SQLiteKernelExtensionTests : IDisposable
    {
        private readonly Configuration _configuration;

        public SQLiteKernelExtensionTests(ITestOutputHelper output)
        {
            _configuration = new Configuration()
                .SetInteractive(Debugger.IsAttached)
                .UsingExtension("json");
        }

        [Fact]
        public async Task can_generate_tabular_json_from_database_table_result()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
                new KeyValueStoreKernel()
            };

            var extension = new SQLiteKernelExtension();

            await extension.OnLoadAsync(kernel);

            using var _ = SqlLiteConnectionTests.CreateInMemorySQLiteDb(out var connectionString);

            await kernel.SubmitCodeAsync(
                $"#!connect --kernel-name mydb sqlite \"{connectionString}\"");

            var result = await kernel.SubmitCodeAsync(@"
#!mydb
SELECT * FROM fruit
");

            var events = result.KernelEvents.ToSubscribedList();

            var formattedData = events.OfType<DisplayedValueProduced>().Single()
                .FormattedValues.Single(fm => fm.MimeType == HtmlFormatter.MimeType)
                .Value;

            this.Assent(formattedData, _configuration);
        }

        [Fact]
        public async Task can_handle_duplicate_columns_in_query_results()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
                new KeyValueStoreKernel()
            };

            var extension = new SQLiteKernelExtension();

            await extension.OnLoadAsync(kernel);

            using var _ = SqlLiteConnectionTests.CreateInMemorySQLiteDb(out var connectionString);

            await kernel.SubmitCodeAsync(
                $"#!connect --kernel-name mydb sqlite \"{connectionString}\"");

            var result = await kernel.SubmitCodeAsync(@"
#!mydb
SELECT 1 AS Apples, 2 AS Bananas, 3 AS Apples, 4 AS BANANAS, 5 AS Apples, 6 AS BaNaNaS
");

            var events = result.KernelEvents.ToSubscribedList();

            var formattedData = events
                                .OfType<DisplayedValueProduced>()
                                .Single()
                                .FormattedValues
                                .Single(fm => fm.MimeType == HtmlFormatter.MimeType)
                                .Value;

            this.Assent(formattedData, _configuration);
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
        }
    }
}