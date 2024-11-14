// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Kusto.Data.Common;
using Microsoft.DotNet.Interactive.SqlServer;
using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Directives;

namespace Microsoft.DotNet.Interactive.Kql;

internal class MsKqlKernel : ToolsServiceKernel
{
    private readonly KqlConnectionDetails _connectionDetails;

    public MsKqlKernel(
        string name,
        KqlConnectionDetails connectionDetails,
        ToolsServiceClient client) : base(name, client, "KQL")
    {
        _connectionDetails = connectionDetails ?? throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionDetails));
        KernelInfo.Description = $"""
                                  Query Kusto cluster {connectionDetails.Cluster} and database {connectionDetails.Database}
                                  """;
    }

    public override async Task ConnectAsync()
    {
        if (!Connected)
        {
            await ServiceClient.ConnectAsync(TempFileUri, _connectionDetails);
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
        if (value is PasswordString ps)
        {
            value = ps.GetClearTextPassword();
        }

        return $"let {name} = {MapToKqlValueDeclaration(value)};";

        static string MapToKqlValueDeclaration(object value) =>
            value switch
            {
                string s => s.AsDoubleQuotedString(),
                char c => c.ToString().AsDoubleQuotedString(),
                _ => value.ToString()
            };
    }

    protected override bool CanDeclareVariable(string name, object value, out string msg)
    {
        msg = default;
        if (value is char)
        {
            // CslType doesn't support char but we just convert it to a string for our use here
            return true;
        }
        try
        {
            var _ = CslType.FromClrType(value.GetType());
        }
        catch (Exception e)
        {
            msg = e.Message;
            return false;
        }
        return true;
    }
}