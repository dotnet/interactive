// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using static Microsoft.DotNet.Interactive.Kernel;

namespace Microsoft.DotNet.Interactive.PowerShell.Commands
{
    using System.Management.Automation;

    /// <summary>
    /// Takes the the JavaScript string input and invokes it on the client.
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "JupyterJavaScript")]
    public sealed class InvokeJupyterJavaScriptCommand : PSCmdlet
    {
        /// <summary>
        /// The JavaScript string to invoke.
        /// </summary>
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public string JavaScriptString { get; set; }

        /// <summary>
        /// ProcessRecord override.
        /// </summary>
        protected override void ProcessRecord()
        {
            Javascript(JavaScriptString);
        }
    }
}
