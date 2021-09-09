// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    public class MsKqlKernel : ToolsServiceKernel
    {
        private readonly KqlConnectionDetails _connectionDetails;

        public MsKqlKernel(
            string name,
            KqlConnectionDetails connectionDetails,
            MsSqlServiceClient client) : base(name, client)
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

        /// <summary>
        /// Map Kusto type to .NET Type equivalent using scalar data types
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/scalar-data-types/">Here</seealso>
        /// <param name="type">Kusto Type</param>
        /// <returns>.NET Equivalent Type</returns>
        protected override Type GetType(string type)
        {
            switch (type)
            {
                case "bool": return Type.GetType("System.Boolean");
                case "datetime": return Type.GetType("System.DateTime");
                case "dynamic": return Type.GetType("System.Object");
                case "guid": return Type.GetType("System.Guid");
                case "int": return Type.GetType("System.Int32");
                case "long": return Type.GetType("System.Int64");
                case "real": return Type.GetType("System.Double");
                case "string": return Type.GetType("System.String");
                case "timespan": return Type.GetType("System.TimeSpan");
                case "decimal": return Type.GetType("System.Data.SqlTypes.SqlDecimal");
                
                default: return typeof(string);
            }
        }

        protected override ChooseKernelDirective CreateChooseKernelDirective() =>
            new ChooseKqlKernelDirective(this);

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