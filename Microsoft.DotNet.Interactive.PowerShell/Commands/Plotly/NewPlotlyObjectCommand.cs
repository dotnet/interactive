// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.DotNet.Interactive.PowerShell.Commands
{
    using System.Management.Automation;

    /// <summary>
    /// Takes the the string input and turns it into an IHtmlContent that can be passed in to
    /// Show-JupyterContent to render Html in a Jupyter cell's output.
    /// </summary>
    [Cmdlet(VerbsCommon.New, "PlotlyObject")]
    [Alias("npo")]
    public sealed class NewPlotlyObjectCommand : PSCmdlet
    {
        /// <summary>
        /// The Trace type you want to create.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateSet(
            "Margin",
            "Xaxis",
            "Yaxis",
            "Up",
            "Font",
            "Center",
            "Eye",
            "Aspectratio",
            "Camera",
            "Zaxis",
            "Scene",
            "Lonaxis",
            "Lataxis",
            "Geo",
            "Legend",
            "Annotation",
            "Shape",
            "Items",
            "Shapes",
            "Radialaxis",
            "Angularaxis",
            "Marker")]
        public string ObjectType { get; set; }

        /// <summary>
        /// EndProcessing override.
        /// </summary>
        protected override void EndProcessing()
        {
            var obj = CreateObject(ObjectType);
            WriteObject(obj);
        }

        private object CreateObject(string objType)
        {
            var fullType = $"{typeof(XPlot.Plotly.Graph).FullName}+{objType}";
            var type = typeof(XPlot.Plotly.Graph).Assembly.GetType(fullType);

            if (type != null)
            {
                return Activator.CreateInstance(type);
            }

            ThrowTerminatingError(
                new ErrorRecord(
                    new InvalidOperationException(
                        $"cannot load trace {objType}"),
                        null,
                        ErrorCategory.ObjectNotFound,
                        null));

            return null;
        }
    }
}
