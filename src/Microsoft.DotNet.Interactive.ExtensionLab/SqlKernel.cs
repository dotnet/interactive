﻿// // Copyright (c) .NET Foundation and contributors. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public abstract class SqlKernel :
        Kernel,
        IKernelCommandHandler<SubmitCode>
    {
        protected SqlKernel(string name) : base(name)
        {
        }

        public virtual async Task HandleAsync(
            SubmitCode submitCode,
            KernelInvocationContext context)
        {
            await using var connection = OpenConnection();

            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            await using var dbCommand = connection.CreateCommand();

            dbCommand.CommandText = submitCode.Code;

            var results = Execute(dbCommand);

            await context.DisplayAsync(results);
        }

        protected abstract DbConnection OpenConnection();

        internal IEnumerable<IEnumerable<IEnumerable<(string name, object value)>>> Execute(IDbCommand command)
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

        internal IEnumerable<SqlRow> Execute2(IDbCommand command)
        {
            using var reader = command.ExecuteReader();

            do
            {
                var values = new object[reader.FieldCount];
                var names = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToArray();

                var row = new SqlRow();

                while (reader.Read())
                {
                    reader.GetValues(values);

                    for (var i = 0; i < values.Length; i++)
                    {
                        row.Add(names[i], values[i]);
                    }
                }

                yield return row;
            } while (reader.NextResult());
        }
    }

    public class SqlRow : Dictionary<string, object>
    {
    }
}