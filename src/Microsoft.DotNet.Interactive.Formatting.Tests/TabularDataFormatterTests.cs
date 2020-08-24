// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Assent;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class TabularDataFormatterTests : IDisposable
    {
        private readonly Configuration _configuration;

        public TabularDataFormatterTests()
        {
            _configuration = new Configuration()
                .SetInteractive(Debugger.IsAttached)
                .UsingExtension("json");
        }

        [Fact]
        public void can_generate_tabular_json_from_object_array()
        {
            var data = new[]
            {
                new { Name = "Q", IsValid = false, Cost = 10.0 },
                new { Name = "U", IsValid = false, Cost = 5.0 },
                new { Name = "E", IsValid = true, Cost = 10.0 },
                new { Name = "S", IsValid = false, Cost = 10.0 },
                new { Name = "T", IsValid = false, Cost = 10.0 }
            };

            var formattedData = data.ToTabularDataSet().ToJson();

            this.Assent(formattedData.ToString(), _configuration);
        }

        [Fact]
        public void can_generate_tabular_json_from_sequence_of_sequences_of_ValueTuples()
        {
            IEnumerable<IEnumerable<(string name, object value)>> data =
                new[]
                {
                    new (string name, object value)[]
                    {
                        ("id", 1),
                        ("name", "apple"),
                        ("color", "green"),
                        ("deliciousness", 10)
                    },
                    new (string name, object value)[]
                    {
                        ("id", 2),
                        ("name", "banana"),
                        ("color", "yellow"),
                        ("deliciousness", 11)
                    },
                    new (string name, object value)[]
                    {
                        ("id", 3),
                        ("name", "cherry"),
                        ("color", "red"),
                        ("deliciousness", 9000)
                    },
                };

            var formattedData = data.ToTabularDataSet().ToJson();

            this.Assent(formattedData.ToString(), _configuration);
        }

        [Fact]
        public void can_generate_tabular_json_from_sequence_of_sequences_of_KeyValuePairs()
        {
            IEnumerable<IEnumerable<KeyValuePair<string, object>>> data =
                new[]
                {
                    new[]
                    {
                        new KeyValuePair<string, object>("id", 1),
                        new KeyValuePair<string, object>("name", "apple"),
                        new KeyValuePair<string, object>("color", "green"),
                        new KeyValuePair<string, object>("deliciousness", 10)
                    },
                    new[]
                    {
                        new KeyValuePair<string, object>("id", 2),
                        new KeyValuePair<string, object>("name", "banana"),
                        new KeyValuePair<string, object>("color", "yellow"),
                        new KeyValuePair<string, object>("deliciousness", 11)
                    },
                    new[]
                    {
                        new KeyValuePair<string, object>("id", 3),
                        new KeyValuePair<string, object>("name", "cherry"),
                        new KeyValuePair<string, object>("color", "red"),
                        new KeyValuePair<string, object>("deliciousness", 9000)
                    },
                };

            var formattedData = data.ToTabularDataSet().ToJson();

            this.Assent(formattedData.ToString(), _configuration);
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
        }

        internal static IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>> Execute(IDbCommand command)
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
    }
}