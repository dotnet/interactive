// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Kusto.Data.Common;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;
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
            if (connectionDetails is null)
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionDetails));
            }

            _connectionDetails = connectionDetails;
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
            new ChooseKqlKernelDirective(this);

        protected override string GenerateVariableDeclaration(KeyValuePair<string, object> variableNameAndValue)
        {
            return $"let {variableNameAndValue.Key} = {MapToKqlValueDeclaration(variableNameAndValue.Value)};";

            static string MapToKqlValueDeclaration(object value) =>
            value switch
            {
                string s => s.AsDoubleQuotedString(),
                char c => c.ToString().AsDoubleQuotedString(),
                _ => value.ToString()
            };
        }

        protected override bool CanSupportVariable(string name, object value, out string msg)
        {
            msg = default;
            if (value.GetType() == typeof(char))
            {
                // CslType doesn't support char but we just convert it to a string for our use here
                return true;
            }
            try
            {
                CslType.FromClrType(value.GetType());
            }
            catch (Exception e)
            {
                msg = e.Message;
                return false;
            }
            return true;
        }

        private class ChooseKqlKernelDirective : ChooseKernelDirective
        {
            public ChooseKqlKernelDirective(Kernel kernel) : base(kernel, $"Run a Kusto query using the \"{kernel.Name}\" connection.")
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
    }
}