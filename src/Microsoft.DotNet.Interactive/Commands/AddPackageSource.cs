// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class AddPackageSource : KernelCommand
{
    public AddPackageSource(
        string packageSource,
        string targetKernelName = null) : base(targetKernelName)
    {
        if (string.IsNullOrWhiteSpace(packageSource))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(packageSource));
        }

        PackageSource = packageSource;
    }

    public string PackageSource { get; }

    internal static Task<KernelCommand> TryParseIDirectiveAsync(
        DirectiveNode directiveNode,
        ExpressionBindingResult bindingResult,
        Kernel kernel)
    {
        AddPackageSource command = null;

        if (directiveNode.TryGetActionDirective(out var directive))
        {
            var parameterValues = directiveNode.GetParameterValues(directive, bindingResult.BoundValues).ToArray();

            var packageSourceParameterResult = parameterValues.SingleOrDefault(v => v.Name is "");

            if (packageSourceParameterResult.Value is string packageSource)
            {
                packageSource = packageSource.Trim(['"']).Replace("nuget:", "");
                command = new AddPackageSource(packageSource);
            }
        }

        return Task.FromResult<KernelCommand>(command);
    }
}