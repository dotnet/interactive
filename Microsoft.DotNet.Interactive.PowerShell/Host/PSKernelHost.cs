// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;

namespace Microsoft.DotNet.Interactive.PowerShell.Host
{
    public class PSKernelHost : PSHost, IHostSupportsInteractiveSession
    {
        private const string HostName = "PowerShell Jupyter Host";

        private readonly Version _hostVersion;
        private readonly Guid _instanceId;
        private readonly PSKernelHostUserInterface _ui;
        private readonly PSObject _consoleColorProxy;

        internal PSKernelHost()
        {
            _hostVersion = new Version("0.0.1");
            _instanceId = Guid.NewGuid();
            _ui = new PSKernelHostUserInterface();
            _consoleColorProxy = PSObject.AsPSObject(new ConsoleColorProxy(_ui));
        }

        #region "PSHost Implementation"

        public override string Name => HostName;

        public override Guid InstanceId => this._instanceId;

        public override Version Version => _hostVersion;

        public override PSHostUserInterface UI => _ui;

        public override PSObject PrivateData => _consoleColorProxy;

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

    internal class ConsoleColorProxy
    {
        private readonly PSKernelHostUserInterface _ui;

        public ConsoleColorProxy(PSKernelHostUserInterface ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public ConsoleColor FormatAccentColor
        {
            get => _ui.FormatAccentColor;
            set => _ui.FormatAccentColor = value;
        }

        public ConsoleColor ErrorAccentColor
        {
            get => _ui.ErrorAccentColor;
            set => _ui.ErrorAccentColor = value;
        }

        public ConsoleColor ErrorForegroundColor
        {
            get => _ui.ErrorForegroundColor;
            set => _ui.ErrorForegroundColor = value;
        }

        public ConsoleColor ErrorBackgroundColor
        {
            get => _ui.ErrorBackgroundColor;
            set => _ui.ErrorBackgroundColor = value;
        }

        public ConsoleColor WarningForegroundColor
        {
            get => _ui.WarningForegroundColor;
            set => _ui.WarningForegroundColor = value;
        }

        public ConsoleColor WarningBackgroundColor
        {
            get => _ui.WarningBackgroundColor;
            set => _ui.WarningBackgroundColor = value;
        }

        public ConsoleColor DebugForegroundColor
        {
            get => _ui.DebugForegroundColor;
            set => _ui.DebugForegroundColor = value;
        }

        public ConsoleColor DebugBackgroundColor
        {
            get => _ui.DebugBackgroundColor;
            set => _ui.DebugBackgroundColor = value;
        }

        public ConsoleColor VerboseForegroundColor
        {
            get => _ui.VerboseForegroundColor;
            set => _ui.VerboseForegroundColor = value;
        }

        public ConsoleColor VerboseBackgroundColor
        {
            get => _ui.VerboseBackgroundColor;
            set => _ui.VerboseBackgroundColor = value;
        }

        public ConsoleColor ProgressForegroundColor
        {
            get => _ui.ProgressForegroundColor;
            set => _ui.ProgressForegroundColor = value;
        }

        public ConsoleColor ProgressBackgroundColor
        {
            get => _ui.ProgressBackgroundColor;
            set => _ui.ProgressBackgroundColor = value;
        }
    }
}
