// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace Microsoft.DotNet.Interactive.PowerShell.Commands
{
    /// <summary>
    /// Connect to Azure PowerShell for code execution.
    /// </summary>
    [Cmdlet(VerbsCommon.Enter, "AzShell")]
    public sealed class EnterAzShellCommand : PSCmdlet
    {
        /// <summary>
        /// ProcessRecord override.
        /// </summary>
        protected override void ProcessRecord()
        {
            var context = KernelInvocationContext.Current;
            if (context?.CurrentKernel is PowerShellKernel psKernel)
            {
                if (Runspace.DefaultRunspace.Id != psKernel.DefaultRunspaceId)
                {
                    var ex = new InvalidOperationException("'Enter-AzShell' should run from the default Runspace of the PowerShell kernel.");
                    var error = new ErrorRecord(
                        exception: ex,
                        errorId: "ShouldRunFromDefaultKernelRunspace",
                        errorCategory: ErrorCategory.InvalidOperation,
                        targetObject: null);
                    this.ThrowTerminatingError(error);
                }

                psKernel.AzShell = new AzShellConnectionUtils(context);
                var windowSize = Host.UI.RawUI.WindowSize;
                psKernel.AzShell.ConnectAndInitializeAzShell(windowSize.Width, windowSize.Height).Wait();
            }
        }
    }
}
