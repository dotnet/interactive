// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Security;

namespace Microsoft.DotNet.Interactive.PowerShell;

using System.Management.Automation;

internal static class PowerShellExtensions
{
    private static readonly PSInvocationSettings _settings = new()
    {
        AddToHistory = true
    };

    public static void InvokeAndClear(this PowerShell pwsh)
    {
        try
        {
            pwsh.Invoke(input: null, settings: _settings);
        }
        finally
        {
            Clear(pwsh);
        }
    }

    public static Collection<PSObject> InvokeAndClearWithResult(this PowerShell pwsh)
    {
        Collection<PSObject> result = null;
        try
        {
            result = pwsh.Invoke(input: null, settings: _settings);
        }
        finally
        {
            Clear(pwsh);
        }

        return result;
    }

    public static void Clear(this PowerShell pwsh)
    {
        pwsh.Streams.ClearStreams();
        pwsh.Commands.Clear();
    }

    internal static SecureString GetSecureStringPassword(this PasswordString pwdString)
    {
        var secure = new SecureString();
        foreach (var c in pwdString.GetClearTextPassword())
        {
            secure.AppendChar(c);
        }

        return secure;
    }

    internal static object Unwrap(this PSObject psObj)
    {
        var obj = psObj.BaseObject;
        if (obj is PSCustomObject)
        {
            var table = new Dictionary<string, object>();
            foreach (var p in psObj.Properties)
            {
                table.Add(p.Name, p.Value);
            }
            obj = table;
        }

        return obj;
    }
}