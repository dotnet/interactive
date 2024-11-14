// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.SqlClient.Server;
using Microsoft.DotNet.Interactive.Directives;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.SqlServer;

internal class MsSqlKernel : ToolsServiceKernel
{
    private readonly string _connectionString;

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
                                  Query a Microsoft SQL database using T-SQL
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

    public override KernelSpecifierDirective KernelSpecifierDirective
    {
        get
        {
            var directive = base.KernelSpecifierDirective;

            directive.Parameters.Add(new("--name"));

            return directive;
        }
    }

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
}