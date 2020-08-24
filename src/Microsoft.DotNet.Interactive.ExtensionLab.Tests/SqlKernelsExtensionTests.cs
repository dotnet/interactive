// // Copyright (c) .NET Foundation and contributors. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Assent;
using Microsoft.Data.SqlClient;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Formatting;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public class SqlKernelsExtensionTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly Configuration _configuration;

        public SqlKernelsExtensionTests(ITestOutputHelper output)
        {
            _output = output;
            _configuration = new Configuration()
                .SetInteractive(Debugger.IsAttached)
                .UsingExtension("json");
        }


        [Fact(Skip = "Requires database")]
        public async Task can_generate_tabular_json_from_database_table_result()
        {
            using var kernel = new CompositeKernel
            {
                new CSharpKernel().UseNugetDirective(),
                new KeyValueStoreKernel()
            };

            var extension = new SqlKernelsExtension();

            await extension.OnLoadAsync(kernel);

            var connectionString = "Persist Security Info=False; Integrated Security=true; Initial Catalog=AdventureWorks2019; Server=localhost";

            await using var connection = new SqlConnection(connectionString);
            connection.Open();

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT TOP 3 * FROM Production.Product";

            var data = Execute(command);

            var formattedData = data.First().ToTabularJsonString();

            this.Assent(formattedData.ToString(), _configuration);
        }

        private static IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>> Execute(IDbCommand command)
        {
            using var reader = command.ExecuteReader();

            do
            {
                var values = new object[reader.FieldCount];
                var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();

                // holds the result of a single statement within the query
                var table = new List<(string, object)[]>();

                while (reader.Read())
                {
                    reader.GetValues(values);
                    var row = new (string, object)[values.Length];
                    for (var i = 0; i < values.Length; i++)
                    {
                        row[i] = (names[i], values[i]);
                    }

                    table.Add(row);
                }

                yield return table;
            } while (reader.NextResult());
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
        }
    }
}