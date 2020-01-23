// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.ObjectModel;

namespace Microsoft.DotNet.Interactive.PowerShell
{
    using System.Management.Automation;

    internal static class PowerShellExtensions
    {
        public static void InvokeAndClearCommands(this PowerShell pwsh)
        {
            try
            {
                pwsh.Invoke();
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
                pwsh.Invoke(input);
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
                var result = pwsh.Invoke<T>();
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
                var result = pwsh.Invoke<T>(input);
                return result;
            }
            finally
            {
                pwsh.Streams.ClearStreams();
                pwsh.Commands.Clear();
            }
        }
    }
}
