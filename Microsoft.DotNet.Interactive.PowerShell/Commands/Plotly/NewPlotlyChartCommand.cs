// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using XPlot.Plotly;
using static XPlot.Plotly.Graph;

namespace Microsoft.DotNet.Interactive.PowerShell.Commands
{
    using System.Management.Automation;

    /// <summary>
    /// Creates a new Plotly Chart object.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PlotlyChart")]
    [OutputType("XPlot.Plotly.Chart")]
    [Alias("npc")]
    public sealed class NewPlotlyChartCommand : PSCmdlet
    {
        /// <summary>
        /// The traces to show in a chart.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public Trace[] Trace { get; set; }

        /// <summary>
        /// The title of the chart.
        /// </summary>
        [Parameter(Position = 1)]
        public string Title { get; set; }  = string.Empty;

        /// <summary>
        /// The title of the chart.
        /// </summary>
        [Parameter(Position = 1)]
        public Layout.Layout Layout { get; set; }

        private List<Trace> _traces;

        /// <summary>
        /// BeginProcessing override.
        /// </summary>
        protected override void BeginProcessing()
        {
            _traces = new List<Trace>();
        }
        
        /// <summary>
        /// ProcessRecord override.
        /// </summary>
        protected override void ProcessRecord()
        {
            _traces.AddRange(Trace);
        }

        /// <summary>
        /// EndProcessing override.
        /// </summary>
        protected override void EndProcessing()
        {
            var chart = Chart.Plot(_traces);
            chart.WithTitle(Title);

            if (Layout != null)
            {
                chart.WithLayout(Layout);
            }

            WriteObject(chart);
        }
    }
}
