// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Data;
using System.Threading.Tasks;

using Microsoft.Data.SqlClient.Server;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive.SqlServer;

internal class MsSqlKernel : ToolsServiceKernel
{
    private readonly string _connectionString;
    private ChooseMsSqlKernelDirective _chooseKernelDirective;

    internal MsSqlKernel(
        string name,
        string connectionString,
        ToolsServiceClient client) : base(name, client, "T-SQL")
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
        }
        KernelInfo.Description = """
                                  This Kernel can execute T-SQL and SQL queries against a SQL Server database.
                                  It is connected to a MSSQL database.
                                  """;
        _connectionString = connectionString;
    }

    public override async Task ConnectAsync()
    {
        if (!Connected)
        {
            await ServiceClient.ConnectAsync(TempFileUri, _connectionString);
            await ConnectionCompleted.Task;
            Connected = true;
        }
    }

    public override ChooseMsSqlKernelDirective ChooseKernelDirective => _chooseKernelDirective ??= new(this);


    protected override string CreateVariableDeclaration(string name, object value)
    {
        return $"DECLARE @{name} {MapToSqlDataType(name, value)} = {MapToSqlValueDeclaration(value)};";

        static string MapToSqlDataType(string name, object value)
        {
            var sqlMetaData = SqlMetaData.InferFromValue(value, name);

            var dbType = sqlMetaData.SqlDbType;

            switch (dbType)
            {
                case SqlDbType.Char:
                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                case SqlDbType.VarChar:
                    return $"{dbType}({sqlMetaData.MaxLength})";
                case SqlDbType.Decimal:
                    return $"{dbType}({sqlMetaData.Precision},{sqlMetaData.Scale})";
                default:
                    return dbType.ToString();
            }
        }

        static string MapToSqlValueDeclaration(object value) =>
            value switch
            {
                string s => $"N{s.AsSingleQuotedString()}",
                char c => $"N{c.ToString().AsSingleQuotedString()}",
                Guid g => $"N{g.ToString().AsSingleQuotedString()}",
                bool b => b ? "1" : "0",
                null => "NULL",
                _ => value.ToString()
            };
    }

    protected override bool CanDeclareVariable(string name, object value, out string msg)
    {
        msg = default;
        try
        {
            SqlMetaData.InferFromValue(
                value,
                name);
        }
        catch (Exception e)
        {
            msg = e.Message;
            return false;
        }

        return true;
    }

    protected override void StoreQueryResults(IReadOnlyCollection<TabularDataResource> results, ParseResult commandKernelChooserParseResult)
    {
        var chooser = ChooseKernelDirective;
        var name = commandKernelChooserParseResult?.GetValueForOption(chooser.NameOption);
        if (!string.IsNullOrWhiteSpace(name))
        {
            StoreQueryResultSet(name, results);
        }
    }
}