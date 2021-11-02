// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Microsoft.DotNet.Interactive.SqlServer;

namespace Microsoft.DotNet.Interactive.Kql
{
    internal class MsKqlKernel : ToolsServiceKernel
    {
        private readonly KqlConnectionDetails _connectionDetails;

        public MsKqlKernel(
            string name,
            KqlConnectionDetails connectionDetails,
            ToolsServiceClient client) : base(name, client)
        {
            _connectionDetails = connectionDetails ?? throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionDetails));
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

        protected override ChooseKernelDirective CreateChooseKernelDirective() =>
            new ChooseMsKqlKernelDirective(this);

        protected override string CreateVariableDeclaration(string name, object value)
        {
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


        protected override void StoreQueryResults(List<TabularDataResource> results, ParseResult commandKernelChooserParseResult)
        {
            var chooser = (ChooseMsKqlKernelDirective)ChooseKernelDirective;
            var name = commandKernelChooserParseResult.ValueForOption(chooser.NameOption);
            if (!string.IsNullOrWhiteSpace(name))
            {
                QueryResults[name] = results;
            }
        }

        private class ChooseMsKqlKernelDirective : ChooseKernelDirective
        {
            public ChooseMsKqlKernelDirective(Kernel kernel) : base(kernel, $"Run a Kusto query using the \"{kernel.Name}\" connection.")
            {
                Add(NameOption);
            }

            public Option<string> NameOption { get; } = new(
                "--name",
                description: "Specify the value name to store the results.",
                getDefaultValue: () => "lastResults");

        }
    }
}