// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.PowerShell
{
    using System.Management.Automation;

    internal static class PowerShellExtensions
    {
        private static PSInvocationSettings _settings = new PSInvocationSettings() { AddToHistory = true };

        public static void InvokeAndClearCommands(this PowerShell pwsh)
        {
            try
            {
                pwsh.Invoke(input: null, _settings);
            }
            finally
            {
                pwsh.Streams.ClearStreams();
                pwsh.Commands.Clear();
            }
        }

        public static async Task InvokeAndClearCommandsAsync(this PowerShell pwsh)
        {
            try
            {
                await pwsh.InvokeAsync<PSObject>(
                    input: null,
                    settings: _settings,
                    callback: null,
                    state: null).ConfigureAwait(false);
            }
            finally
            {
                pwsh.Streams.ClearStreams();
                pwsh.Commands.Clear();
            }
        }

        public static void InvokeAndClearCommands(this PowerShell pwsh, IEnumerable input)
        {
            try
            {
                pwsh.Invoke(input, _settings);
            }
            finally
            {
                pwsh.Streams.ClearStreams();
                pwsh.Commands.Clear();
            }
        }

        public static Collection<T> InvokeAndClearCommands<T>(this PowerShell pwsh)
        {
            try
            {
                var result = pwsh.Invoke<T>(input: null, settings: _settings);
                return result;
            }
            finally
            {
                pwsh.Streams.ClearStreams();
                pwsh.Commands.Clear();
            }
        }

        public static Collection<T> InvokeAndClearCommands<T>(this PowerShell pwsh, IEnumerable input)
        {
            try
            {
                var result = pwsh.Invoke<T>(input, _settings);
                return result;
            }
            finally
            {
                pwsh.Streams.ClearStreams();
                pwsh.Commands.Clear();
            }
        }

        internal static SecureString GetSecureStringPassword(this PasswordString pwdString)
        {
            var secure = new SecureString();
            foreach (char c in pwdString.GetClearTextPassword())
            {
                secure.AppendChar(c);
            }

            return secure;
        }

        internal static object Unwrap(this PSObject psObj)
        {
            object obj = psObj.BaseObject;
            if (psObj.BaseObject is PSCustomObject)
            {
                Dictionary<string, object> table = new Dictionary<string, object>();
                foreach (var p in psObj.Properties)
                {
                    table.Add(p.Name, p.Value);
                }
                obj = table;
            }

            return obj;
        }
    }
}
