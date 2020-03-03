// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.PowerShell.Host
{
    public partial class PSKernelHostUserInterface : PSHostUserInterface, IHostUISupportsMultipleChoiceSelection
    {
        private readonly PSKernelHostRawUserInterface _rawUI;
        private readonly object _instanceLock;

        internal PSKernelHostUserInterface()
        {
            _rawUI = new PSKernelHostRawUserInterface();
            _instanceLock = new object();
        }

        public override PSHostRawUserInterface RawUI => _rawUI;

        public override bool SupportsVirtualTerminal => true;

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName)
        {
            return PromptForCredential(caption, message, userName, targetName, PSCredentialTypes.Default, PSCredentialUIOptions.Default);
        }

        public override PSCredential PromptForCredential(string caption, string message, string userName, string targetName, PSCredentialTypes allowedCredentialTypes, PSCredentialUIOptions options)
        {
            if (!string.IsNullOrEmpty(caption))
            {
                WriteLine();
                WriteLine(PromptColor, RawUI.BackgroundColor, caption);
            }

            if (!string.IsNullOrEmpty(message))
            {
                WriteLine(message);
            }

            if (string.IsNullOrEmpty(userName))
            {
                do
                {
                    // Prompt for user name first.
                    userName = ReadInput("User: ");
                }
                while (userName.Length == 0);
            }

            // Prompt for the password now.
            string pwdPrompt = $"Password for user {userName}: ";
            var password = ReadPassword(pwdPrompt).GetSecureStringPassword();

            WriteLine();
            return new PSCredential(userName, password);
        }

        private string ReadInput(string prompt)
        {
            var context = KernelInvocationContext.Current;
            if (context?.CurrentKernel is PowerShellKernel psKernel && psKernel.ReadInput != null)
            {
                return psKernel.ReadInput(prompt);
            }

            throw new InvalidOperationException($"'{nameof(ReadInput)}' should be called from PowerShell kernel.");
        }

        private PasswordString ReadPassword(string prompt)
        {
            var context = KernelInvocationContext.Current;
            if (context?.CurrentKernel is PowerShellKernel psKernel && psKernel.ReadPassword != null)
            {
                return psKernel.ReadPassword(prompt);
            }

            throw new InvalidOperationException($"'{nameof(ReadPassword)}' should be called from PowerShell kernel.");
        }

        public override string ReadLine()
        {
            return ReadInput(prompt: string.Empty);
        }

        public override SecureString ReadLineAsSecureString()
        {
            return ReadPassword(prompt: string.Empty).GetSecureStringPassword();
        }

        public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            // Colorize the text if the text is not already decorated with escape sequences.
            string vtSeqs = value.IndexOf(VTColorUtils.EscapeCharacters) == -1
                ? VTColorUtils.CombineColorSequences(foregroundColor, backgroundColor)
                : string.Empty;

            if (string.IsNullOrEmpty(vtSeqs))
            {
                Console.Write(value);
            }
            else
            {
                Console.Write($"{vtSeqs}{value}{VTColorUtils.ResetColor}");
            }
        }

        public override void Write(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            Write(RawUI.ForegroundColor, RawUI.BackgroundColor, value);
        }

        public override void WriteLine()
        {
            Console.WriteLine();
        }

        public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
        {
            // Colorize the text if the text is not already decorated with escape sequences.
            string vtSeqs = value.IndexOf(VTColorUtils.EscapeCharacters) == -1
                ? VTColorUtils.CombineColorSequences(foregroundColor, backgroundColor)
                : string.Empty;

            if (string.IsNullOrEmpty(vtSeqs))
            {
                Console.WriteLine(value);
            }
            else
            {
                Console.WriteLine($"{vtSeqs}{value}{VTColorUtils.ResetColor}");
            }
        }

        public override void WriteLine(string value)
        {
            WriteLine(RawUI.ForegroundColor, RawUI.BackgroundColor, value);
        }

        public override void WriteInformation(InformationRecord record)
        {
            // Do nothing. The information stream is not visible by default
        }

        public override void WriteErrorLine(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            WriteLine(ErrorForegroundColor, ErrorBackgroundColor, value);
        }

        public override void WriteDebugLine(string message)
        {
            WriteLine(DebugForegroundColor, DebugBackgroundColor, $"DEBUG: {message}");
        }

        public override void WriteVerboseLine(string message)
        {
            WriteLine(VerboseForegroundColor, VerboseBackgroundColor, $"VERBOSE: {message}");
        }

        public override void WriteWarningLine(string message)
        {
            WriteLine(WarningForegroundColor, WarningBackgroundColor, $"WARNING: {message}");
        }

        // Format colors
        public ConsoleColor FormatAccentColor { get; set; } = ConsoleColor.Green;

        // Error colors
        public ConsoleColor ErrorAccentColor { get; set; } = ConsoleColor.Cyan;
        public ConsoleColor ErrorForegroundColor { get; set; } = ConsoleColor.Red;
        public ConsoleColor ErrorBackgroundColor { get; set; } = VTColorUtils.DefaultConsoleColor;

        // Warning colors
        public ConsoleColor WarningForegroundColor { get; set; } = ConsoleColor.Yellow;
        public ConsoleColor WarningBackgroundColor { get; set; } = VTColorUtils.DefaultConsoleColor;

        // Debug colors
        public ConsoleColor DebugForegroundColor { get; set; } = ConsoleColor.Yellow;
        public ConsoleColor DebugBackgroundColor { get; set; } = VTColorUtils.DefaultConsoleColor;

        // Verbose colors
        public ConsoleColor VerboseForegroundColor { get; set; } = ConsoleColor.Yellow;
        public ConsoleColor VerboseBackgroundColor { get; set; } = VTColorUtils.DefaultConsoleColor;

        // Progress colors
        public ConsoleColor ProgressForegroundColor { get; set; } = ConsoleColor.Yellow;
        public ConsoleColor ProgressBackgroundColor { get; set; } = ConsoleColor.DarkCyan;
    }
}
