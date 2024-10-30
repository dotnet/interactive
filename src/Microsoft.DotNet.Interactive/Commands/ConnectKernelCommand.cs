// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Commands;

/// <summary>
/// Defines a magic command that can be used to connect a subkernel dynamically.
/// </summary>
public abstract class ConnectKernelCommand : KernelDirectiveCommand
{
    protected ConnectKernelCommand(string connectedKernelName)
    {
        ConnectedKernelName = connectedKernelName;
    }

    [JsonInclude]
    [JsonPropertyName("kernelName")]
    public string ConnectedKernelName { get; private set; }

    public override IEnumerable<string> GetValidationErrors(CompositeKernel kernel)
    {
        List<string> errors = new();

        errors.AddRange(base.GetValidationErrors(kernel));

        var foundDuplicate = false;

        kernel.RootKernel.VisitSubkernelsAndSelf(k =>
        {
            if (k.KernelInfo.NameAndAliases.Contains(ConnectedKernelName))
            {
                foundDuplicate = true;
            }
        });

        if (foundDuplicate)
        {
            errors.Add($"The kernel name or alias '{ConnectedKernelName}' is already in use.");
        }

        return errors;
    }
}