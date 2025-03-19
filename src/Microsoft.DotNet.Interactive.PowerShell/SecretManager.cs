// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using Pocket;
using static Pocket.Logger;

namespace Microsoft.DotNet.Interactive.PowerShell;

public class SecretManager
{
    private readonly PowerShellKernel _kernel;
    private bool _initialized;

    public SecretManager(PowerShellKernel kernel)
    {
        if (kernel.KernelInfo.IsProxy)
        {
            throw new InvalidOperationException("Cannot create a SecretManager for a proxy kernel.");
        }

        _kernel = kernel;
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        var code = $"""
                    Get-SecretVault -Name '{VaultName}' | Out-Null
                    """;

        if (!_kernel.RunLocally(code, out var errorMessage, true))
        {
            // initialize the SecretVault
            // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "This is a default value used to bootstrap the secret manager.")]
            const string defaultPassword = "P@ssW0rD!";
            var registrationCode =
                $$"""
                  Register-SecretVault -Name {{VaultName}} -ModuleName Microsoft.PowerShell.SecretStore
                   
                  $storeConfiguration = @{
                      Authentication = 'None'
                      Interaction = 'None'
                      Password = ConvertTo-SecureString "{{defaultPassword}}" -AsPlainText -Force
                      Confirm = $false
                  }
                  Set-SecretStoreConfiguration @storeConfiguration
                  """;

            if (!_kernel.RunLocally(registrationCode, out errorMessage, true))
            {
                throw new InvalidOperationException(errorMessage);
            }

            _initialized = true;
        }
    }

    public string VaultName => "DotnetInteractive";

    public void SetValue(
        string name,
        string value)
    {
        EnsureInitialized();

        var code =
            $$"""
              Set-Secret -Name '{{name}}' -Secret '{{value}}' -Vault '{{VaultName}}'
              """;

        if (!_kernel.RunLocally(code, out var errorMessage, true))
        {
            Log.Error(errorMessage);
        }
    }

    public bool TryGetValue(string name, out string? value)
    {
        EnsureInitialized();

        var temporaryVariableName = Guid.NewGuid().ToString("N");

        try
        {
            var code = $"""
                        ${temporaryVariableName} = Get-Secret -Name '{name}' -Vault '{VaultName}' -AsPlainText
                        """;

            if (!_kernel.RunLocally(code, out _, true))
            {
                value = null;
                return false;
            }

            if (_kernel.TryGetValue(temporaryVariableName, out value))
            {
                return true;
            }
        }
        finally
        {
            if (!_kernel.RunLocally($"""
                                     Remove-Variable -Name '{temporaryVariableName}'
                                     """,
                                    out _, true))
            {
            }
        }

        return false;
    }
}