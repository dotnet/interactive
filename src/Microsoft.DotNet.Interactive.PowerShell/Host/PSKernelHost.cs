// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;

namespace Microsoft.DotNet.Interactive.PowerShell.Host;

public class PSKernelHost : PSHost, IHostSupportsInteractiveSession
{
    private readonly PowerShellKernel _powerShell;
    private const string HostName = ".NET Interactive Host";

    private readonly PSKernelHostUserInterface _ui;

    internal PSKernelHost(PowerShellKernel powerShell)
    {
        _powerShell = powerShell ?? throw new ArgumentNullException(nameof(powerShell));
        Version = new Version("1.0.0");
        InstanceId = Guid.NewGuid();
        _ui = new PSKernelHostUserInterface(_powerShell);
        PrivateData = PSObject.AsPSObject(new ConsoleColorProxy(_ui));
    }

    #region "PSHost Implementation"

    public override string Name => HostName;

    public override Guid InstanceId { get; }

    public override Version Version { get; }

    public override PSHostUserInterface UI => _ui;

    public override PSObject PrivateData { get; }

    public override CultureInfo CurrentCulture => CultureInfo.CurrentCulture;

    public override CultureInfo CurrentUICulture => CultureInfo.CurrentUICulture;

    public override void EnterNestedPrompt()
    {
        throw new PSNotImplementedException("Nested prompt support is coming soon.");
    }

    public override void ExitNestedPrompt()
    {
        throw new PSNotImplementedException("Nested prompt support is coming soon.");
    }

    public override void NotifyBeginApplication()
    {
        // Don't need to do anything for the PS kernel.
    }

    public override void NotifyEndApplication()
    {
        // Don't need to do anything for the PS kernel.
    }

    public override void SetShouldExit(int exitCode)
    {
        // Don't need to do anything for the PS kernel.
    }

    #endregion

    #region "IHostSupportsInteractiveSession Implementation"

    public bool IsRunspacePushed => false;

    public Runspace Runspace => throw new PSNotImplementedException("IHostSupportsInteractiveSession support is coming soon.");

    public void PopRunspace()
    {
        throw new PSNotImplementedException("IHostSupportsInteractiveSession support is coming soon.");
    }

    public void PushRunspace(Runspace runspace)
    {
        throw new PSNotImplementedException("IHostSupportsInteractiveSession support is coming soon.");
    }

    #endregion
}