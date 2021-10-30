// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient.Server;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    internal class MsSqlKernel : ToolsServiceKernel
    {
        private readonly string _connectionString;

        internal MsSqlKernel(
            string name,
            string connectionString,
            ToolsServiceClient client) : base(name, client)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));
            }
            
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

        protected override ChooseKernelDirective CreateChooseKernelDirective() =>
            new ChooseMsSqlKernelDirective(this);

        private class ChooseMsSqlKernelDirective : ChooseKernelDirective
        {
            public ChooseMsSqlKernelDirective(Kernel kernel) : base(kernel, $"Run a T-SQL query using the \"{kernel.Name}\" connection.")
            {
                Add(MimeTypeOption);
            }

            private Option<string> MimeTypeOption { get; } = new(
                "--mime-type",
                description: "Specify the MIME type to use for the data.",
                getDefaultValue: () => HtmlFormatter.MimeType);

            protected override async Task Handle(KernelInvocationContext kernelInvocationContext, InvocationContext commandLineInvocationContext)
            {
                await base.Handle(kernelInvocationContext, commandLineInvocationContext);

                switch (kernelInvocationContext.Command)
                {
                    case SubmitCode c:
                        var mimeType = commandLineInvocationContext.ParseResult.ValueForOption(MimeTypeOption);

                        c.Properties.Add("mime-type", mimeType);
                        break;
                }
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
}