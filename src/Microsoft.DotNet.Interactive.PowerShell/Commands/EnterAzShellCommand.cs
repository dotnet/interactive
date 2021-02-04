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
    [Cmdlet(VerbsCommon.Enter, "AzShell", DefaultParameterSetName = DefaultSetName)]
    public sealed class EnterAzShellCommand : PSCmdlet
    {
        private const string TenantIdSetName = "TenantIdSpecified";
        private const string DefaultSetName = "Default";

        [Parameter(ParameterSetName = TenantIdSetName)]
        public Guid TenantId { get; set; }

        [Parameter(ParameterSetName = DefaultSetName)]
        public SwitchParameter Reset;

        /// <summary>
        /// ProcessRecord override.
        /// </summary>
        protected override void ProcessRecord()
        {
            var context = KernelInvocationContext.Current;
            if (context?.HandlingKernel is PowerShellKernel psKernel)
            {
                if (Runspace.DefaultRunspace.Id != psKernel.DefaultRunspaceId)
                {
                    var ex = new InvalidOperationException("'Enter-AzShell' should run from the default Runspace of the PowerShell kernel.");
                    var error = new ErrorRecord(
                        exception: ex,
                        errorId: "ShouldRunFromDefaultKernelRunspace",
                        errorCategory: ErrorCategory.InvalidOperation,
                        targetObject: null);
                    ThrowTerminatingError(error);
                }

                var azShell = ParameterSetName == TenantIdSetName
                    ? new AzShellConnectionUtils(TenantId != Guid.Empty ? TenantId.ToString() : null)
                    : new AzShellConnectionUtils(Reset.IsPresent);

                var windowSize = Host.UI.RawUI.WindowSize;
                bool success = azShell.ConnectAndInitializeAzShell(windowSize.Width, windowSize.Height).GetAwaiter().GetResult();

                if (success)
                {
                    psKernel.AzShell = azShell;
                    psKernel.RegisterForDisposal(azShell);
                }
                else
                {
                    azShell.Dispose();
                }
            }
        }
    }
}
