// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class AddPackage : KernelCommand
{
    public AddPackage(
        string packageName,
        string packageVersion = null,
        string targetKernelName = null) : base(targetKernelName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageName));
        }

        PackageName = packageName;
        PackageVersion = packageVersion ?? string.Empty;
    }

    public string PackageName { get; }

    public string PackageVersion { get; }

    internal static Task<KernelCommand> TryParseRDirectiveAsync(
        DirectiveNode directiveNode,
        ExpressionBindingResult bindingResult,
        Kernel kernel)
    {
        AddPackage command = null;

        if (directiveNode.TryGetActionDirective(out var directive))
        {
            var parameterValues = directiveNode.GetParameterValues(directive, bindingResult.BoundValues).ToArray();

            var packageAndVersion = parameterValues.SingleOrDefault(v => v.Name is "");

            if (packageAndVersion.Value is string packageAndVersionValue)
            {
                if (PackageReference.TryParse(packageAndVersionValue, out var packageReference))
                {
                    command = new AddPackage(packageReference.PackageName, packageReference.PackageVersion);
                }
            }
        }

        return Task.FromResult<KernelCommand>(command);
    }
}