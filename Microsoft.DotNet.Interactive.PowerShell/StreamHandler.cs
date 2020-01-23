// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.PowerShell
{
    using System.Management.Automation;

    internal class StreamHandler
    {
        private static readonly PowerShell s_powerShell = PowerShell.Create();
        private static readonly object s_powerShellLock = new object();
        KernelInvocationContext _context;
        IKernelCommand _command;

        static StreamHandler()
        {
            SetupRecordFormatters();
        }

        public StreamHandler(KernelInvocationContext context, IKernelCommand command)
        {
            _context = context;
            _command = command;
        }

        public void DebugDataAdding(object sender, DataAddingEventArgs e)
        {
            if(e.ItemAdded is DebugRecord record)
            {
                PublishStreamRecord(record, _context, _command);
            }
        }

        public void ErrorDataAdding(object sender, DataAddingEventArgs e)
        {
            if(e.ItemAdded is ErrorRecord record)
            {
                PublishStreamRecord(record, _context, _command);
            }
        }

        public void InformationDataAdding(object sender, DataAddingEventArgs e)
        {
            if(e.ItemAdded is InformationRecord record)
            {
                PublishStreamRecord(record, _context, _command);
            }
        }

        public void ProgressDataAdding(object sender, DataAddingEventArgs e)
        {
            if(e.ItemAdded is ProgressRecord record)
            {
                PublishStreamRecord(record, _context, _command);
            }
        }

        public void VerboseDataAdding(object sender, DataAddingEventArgs e)
        {
            if(e.ItemAdded is VerboseRecord record)
            {
                PublishStreamRecord(record, _context, _command);
            }
        }

        public void WarningDataAdding(object sender, DataAddingEventArgs e)
        {
            if(e.ItemAdded is WarningRecord record)
            {
                PublishStreamRecord(record, _context, _command);
            }
        }

        internal static void PublishStreamRecord(
            object output,
            KernelInvocationContext context,
            IKernelCommand command)
        {
            context.Publish(
                new DisplayedValueProduced(
                    output,
                    command,
                    FormattedValue.FromObject(output)));
        }

        private static void SetupRecordFormatters()
        {
            // DebugRecord
            Formatter<DebugRecord>.Register((record, writer) => {
                PocketView view = PocketViewTags.pre($"DEBUG: {record.Message}");
                writer.WriteLine(view.ToDisplayString(HtmlFormatter.MimeType));
            }, HtmlFormatter.MimeType);

            Formatter<DebugRecord>.Register((record, writer) => {
                writer.WriteLine($"DEBUG: {record.Message}");
            }, PlainTextFormatter.MimeType);

            // ErrorRecord
            Formatter<ErrorRecord>.Register((record, writer) => {
                string result = null;
                lock(s_powerShellLock)
                {
                    var errorDetails = s_powerShell.AddCommand("Microsoft.PowerShell.Utility\\Out-String")
                        .AddParameter("InputObject", record)
                        .InvokeAndClearCommands<string>();
                    result = errorDetails.Single();
                }

                if (result != null)
                {
                    PocketView view = PocketViewTags.pre(result);
                    writer.WriteLine(view.ToDisplayString(HtmlFormatter.MimeType));
                }
            }, HtmlFormatter.MimeType);

            Formatter<ErrorRecord>.Register((record, writer) => {
                string result = null;
                lock(s_powerShellLock)
                {
                    var errorDetails = s_powerShell.AddCommand("Microsoft.PowerShell.Utility\\Out-String")
                        .AddParameter("InputObject", record)
                        .InvokeAndClearCommands<string>();
                    result = errorDetails.Single();
                }

                if (result != null)
                {
                    writer.WriteLine(result);
                }
            }, PlainTextFormatter.MimeType);

            // InformationRecord
            Formatter<InformationRecord>.Register((record, writer) => {
                string prefix = (record.Tags.Count == 1 && record.Tags[0] == "__PipelineObject__") ? "" : "INFORMATION:";
                PocketView view = PocketViewTags.pre($"{prefix} {record.MessageData}");
                writer.WriteLine(view.ToDisplayString(HtmlFormatter.MimeType));
            }, HtmlFormatter.MimeType);

            Formatter<InformationRecord>.Register((record, writer) => {
                string prefix = (record.Tags.Count == 1 && record.Tags[0] == "__PipelineObject__") ? "" : "INFORMATION:";
                writer.WriteLine($"{prefix} {record.MessageData}");
            }, PlainTextFormatter.MimeType);

            // ProgressRecord
            Formatter<ProgressRecord>.Register((record, writer) => {
                PocketView view = PocketViewTags.pre($"PROGRESS: {record.StatusDescription}");
                writer.WriteLine(view.ToDisplayString(HtmlFormatter.MimeType));
            }, HtmlFormatter.MimeType);

            Formatter<ProgressRecord>.Register((record, writer) => {
                writer.WriteLine($"PROGRESS: {record.StatusDescription}");
            }, PlainTextFormatter.MimeType);            

            // VerboseRecord
            Formatter<VerboseRecord>.Register((record, writer) => {
                PocketView view = PocketViewTags.pre($"VERBOSE: {record.Message}");
                writer.WriteLine(view.ToDisplayString(HtmlFormatter.MimeType));
            }, HtmlFormatter.MimeType);

            Formatter<VerboseRecord>.Register((record, writer) => {
                writer.WriteLine($"VERBOSE: {record.Message}");
            }, PlainTextFormatter.MimeType);

            // WarningRecord
            Formatter<WarningRecord>.Register((record, writer) => {
                PocketView view = PocketViewTags.pre($"WARNING: {record.Message}");
                writer.WriteLine(view.ToDisplayString(HtmlFormatter.MimeType));
            }, HtmlFormatter.MimeType);

            Formatter<WarningRecord>.Register((record, writer) => {
                writer.WriteLine($"WARNING: {record.Message}");
            }, PlainTextFormatter.MimeType);
        }
    }
}
