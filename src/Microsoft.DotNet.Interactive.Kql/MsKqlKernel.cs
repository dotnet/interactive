// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

using Kusto.Data.Common;

using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.SqlServer;

namespace Microsoft.DotNet.Interactive.Kql;

internal class MsKqlKernel : ToolsServiceKernel
{
    private readonly KqlConnectionDetails _connectionDetails;
    private ChooseMsKqlKernelDirective _chooseKernelDirective;

    public MsKqlKernel(
        string name,
        KqlConnectionDetails connectionDetails,
        ToolsServiceClient client) : base(name, client, "KQL")
    {
        _connectionDetails = connectionDetails ?? throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionDetails));
        KernelInfo.Description = $"""
                                  This Kernel can execute KQL queries against a Kusto database. 
                                  This instance is connected to cluster {connectionDetails.Cluster} and database {connectionDetails.Database}.
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

    public override ChooseMsKqlKernelDirective ChooseKernelDirective => _chooseKernelDirective ??= new(this);

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