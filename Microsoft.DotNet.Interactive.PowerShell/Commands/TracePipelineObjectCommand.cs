//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.PowerShell.Commands;

namespace Microsoft.DotNet.Interactive.PowerShell.Commands
{
    using System.Management.Automation;

    /// <summary>
    /// Writes the formatted output of the pipeline object to the information stream before passing the object down to the pipeline.
    /// </summary>
    /// <remarks>
    /// This Cmdlet behaves like 'Tee-Object'.
    /// An input pipeline object is first pushed through a steppable pipeline that consists of 'Out-String | Write-Information -Tags "__PipelineObject__"',
    /// and then it's written out back to the pipeline without change. In this approach, we can intercept and trace the pipeline objects in a streaming way
    /// and keep the objects in pipeline at the same time.
    /// </remarks>
    [Cmdlet(VerbsDiagnostic.Trace, "PipelineObject")]
    public sealed class TracePipelineObjectCommand : PSCmdlet
    {
        /// <summary>
        /// The object from pipeline.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public object InputObject { get; set; }

        private static PowerShell s_pwsh;
        private SteppablePipeline _stepPipeline;

        static TracePipelineObjectCommand()
        {
            s_pwsh = PowerShell.Create();
            s_pwsh.AddCommand(CommandUtils.OutStringCmdletInfo).AddParameter("Stream", CommandUtils.BoxedTrue)
                  .AddCommand(CommandUtils.WriteInformationCmdletInfo).AddParameter("Tags", "__PipelineObject__");
        }

        /// <summary>
        /// BeginProcessing override.
        /// </summary>
        protected override void BeginProcessing()
        {
            _stepPipeline = s_pwsh.GetSteppablePipeline();
            _stepPipeline.Begin(this);
        }

        /// <summary>
        /// ProcessRecord override.
        /// </summary>
        protected override void ProcessRecord()
        {
            _stepPipeline.Process(InputObject);
            WriteObject(InputObject);
        }

        /// <summary>
        /// EndProcessing override.
        /// </summary>
        protected override void EndProcessing()
        {
            _stepPipeline.End();
        }
    }
}
