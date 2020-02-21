// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using static Microsoft.DotNet.Interactive.Kernel;

namespace Microsoft.DotNet.Interactive.PowerShell.Commands
{
    using System.Management.Automation;
    using Microsoft.DotNet.Interactive.Events;

    /// <summary>
    /// Takes the the input and displays it on the client using .NET Interactive's formatters.
    /// This returns a DisplayedValue that can then be updated by calling `Update` on the object.
    /// </summary>
    [Cmdlet(VerbsCommon.Show, "JupyterContent")]
    [OutputType("Interactive.Events.DisplayedValue")]
    public sealed class ShowJupyterContentCommand : PSCmdlet
    {
        /// <summary>
        /// The object from pipeline.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        [AllowNull]
        public object InputObject { get; set; }

        /// <summary>
        /// The MimeType to use.
        /// </summary>
        [Parameter(Position = 1)]
        public string MimeType { get; set; }

        /// <summary>
        /// Determines whether the DisplayedValue should get written to the pipeline.
        /// </summary>
        [Parameter()]
        public SwitchParameter PassThru { get; set; }

        /// <summary>
        /// ProcessRecord override.
        /// </summary>
        protected override void ProcessRecord()
        {
            object obj = InputObject is PSObject psObject ? psObject.BaseObject : InputObject;
            DisplayedValue displayedValue = display(obj, MimeType);
            
            if (PassThru.IsPresent)
            {
                WriteObject(displayedValue);
            }
        }
    }
}
