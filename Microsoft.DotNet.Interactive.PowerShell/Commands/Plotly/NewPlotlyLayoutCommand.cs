// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using static XPlot.Plotly.Layout;

namespace Microsoft.DotNet.Interactive.PowerShell.Commands
{
    using System.Management.Automation;

    /// <summary>
    /// Takes the the string input and turns it into an IHtmlContent that can be passed in to
    /// Show-JupyterContent to render Html in a Jupyter cell's output.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PlotlyLayout")]
    [OutputType("XPlot.Plotly.Layout+Layout")]
    [Alias("npl")]
    public sealed class NewPlotlyLayoutCommand : PSCmdlet
    {
        /// <summary>
        /// The title of the chart.
        /// </summary>
        [Parameter(Position = 0)]
        public string Title { get; set; }  = string.Empty;

        /// <summary>
        /// EndProcessing override.
        /// </summary>
        protected override void EndProcessing()
        {
            var layout = new Layout();
            layout.title = Title;
            WriteObject(layout);
        }
    }
}
