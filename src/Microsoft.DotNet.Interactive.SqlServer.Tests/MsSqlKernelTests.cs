// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.SqlServer.Tests
{
    public class MsSqlKernelTests
    {
        public static IEnumerable<object[]> EmptyResultSetTestData =>
            new List<object[]>
            {
                new object[] { null },
                new object[] { new CellValue[][] { } }
            };

        [Theory]
        [MemberData(nameof(EmptyResultSetTestData))]
        public void Should_display_single_row_for_empty_result_sets(CellValue[][] rows)
        {
            var columnInfo = new ColumnInfo[]
            {
                new ColumnInfo
                {
                    ColumnName = "apples"
                },
                new ColumnInfo
                {
                    ColumnName = "bananas"
                },
                new ColumnInfo
                {
                    ColumnName = "oranges"
                }
            };
            var enumerableTable = MsSqlKernel.GetEnumerableTables(columnInfo, rows);
            
            int tableCount = 0;
            int rowsCount = 0;
            var cellValues = new List<(string, object)>();
            foreach (var table in enumerableTable)
            {
                tableCount++;
                foreach (var tableRow in table)
                {
                    rowsCount++;
                    foreach (var tableCell in tableRow)
                    {
                        cellValues.Add(tableCell);
                    }
                }
            }

            tableCount.Should().Be(1);
            rowsCount.Should().Be(1);
            cellValues.Count.Should().Be(columnInfo.Length);
            for (int i = 0; i < cellValues.Count; i++)
            {
                cellValues[i].Item1.Should().Be(columnInfo[i].ColumnName);
                cellValues[i].Item2.Should().Be(null);
            }
        }
    }
}