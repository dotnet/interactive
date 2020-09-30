// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public class SqlLiteConnectionTests
    {
        private readonly ITestOutputHelper _output;

        public SqlLiteConnectionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task It_can_connect_and_query_data()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
                new KeyValueStoreKernel()
            };

            kernel.UseKernelClientConnection(new SQLiteKernelConnection());

            using var _ = CreateInMemorySQLiteDb(out var connectionString);

            var result = await kernel.SubmitCodeAsync(
                             $"#!connect --kernel-name mydb sqlite \"{connectionString}\"");

            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .NotContainErrors();

            result = await kernel.SubmitCodeAsync(@"
#!mydb
SELECT * FROM fruit
");

            var events = result.KernelEvents.ToSubscribedList();

            events.Should().NotContainErrors();

            events.Should()
                  .ContainSingle<DisplayedValueProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle(f => f.MimeType == HtmlFormatter.MimeType);
        }

        internal static IDisposable CreateInMemorySQLiteDb(out string connectionString)
        {
            connectionString = $"Data Source=InMemorySample_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
            var topLevelConnection = new SqliteConnection(connectionString);
            topLevelConnection.Open();

            var createCommand = topLevelConnection.CreateCommand();
            createCommand.CommandText =
                @"
DROP TABLE IF EXISTS fruit;  
CREATE TABLE fruit (
    id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    color TEXT NOT NULL,
    deliciousness INT NOT NULL 
);
            ";
            createCommand.ExecuteNonQuery();

            using var connection = new SqliteConnection(connectionString);

            connection.Open();

            var updateCommand = connection.CreateCommand();
            updateCommand.CommandText =
                @"
INSERT INTO fruit (name, color, deliciousness)
VALUES ('apple', 'green', 10);

INSERT INTO fruit (name, color, deliciousness)
VALUES ('banana', 'red', 11);

INSERT INTO fruit (name, color, deliciousness)
VALUES ('cherry', 'red', 9000);
                ";
            updateCommand.ExecuteNonQuery();

            return topLevelConnection;
        }
    }
}