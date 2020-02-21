// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using static XPlot.Plotly.Graph;

namespace Microsoft.DotNet.Interactive.PowerShell.Commands
{
    using System.Management.Automation;

    /// <summary>
    /// Takes the the string input and turns it into an IHtmlContent that can be passed in to
    /// Show-JupyterContent to render Html in a Jupyter cell's output.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PlotlyTrace")]
    [OutputType("XPlot.Plotly.Graph+Trace")]
    public sealed class NewPlotlyTraceCommand : PSCmdlet
    {
        /// <summary>
        /// The object from pipeline.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        public string TraceType { get; set; }

        [Parameter(Position = 1)]
        public string Name { get; set; } = "Untitled";

        protected override void EndProcessing()
        {
            var trace = CreateTrace(TraceType);
            trace.name = Name;
            WriteObject(trace);
        }

        private Trace CreateTrace(string traceType)
        {
            var fullType = $"{typeof(XPlot.Plotly.Graph).FullName}+{traceType}";
            var type = typeof(XPlot.Plotly.Graph).Assembly.GetType(fullType);
            
            if (type != null)
            {
                return Activator.CreateInstance(type) as Trace;
            }
            
            ThrowTerminatingError(
                new ErrorRecord(
                    new InvalidOperationException(
                        $"cannot load trace {traceType}"),
                        null,
                        ErrorCategory.ObjectNotFound,
                        null));
            
            return null;
        }
    }
}
