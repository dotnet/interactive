// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.SqlServer
{
    public class MsSqlKernel : ToolsServiceKernel
    {
        private readonly string _connectionString;

        public MsSqlKernel(
            string name,
            string connectionString,
            MsSqlServiceClient client) : base(name, client)
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

        protected override Type GetType(string typeName)
        {
            return Type.GetType(typeName);
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
                        var mimeType = commandLineInvocationContext.ParseResult.FindResultFor(MimeTypeOption)?.GetValueOrDefault();

                        c.Properties.Add("mime-type", mimeType);
                        break;
                }
            }
        }
    }
}