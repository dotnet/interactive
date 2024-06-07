// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class ImportDocument : KernelCommand
{
    public ImportDocument(
        string filePath,
        string targetKernelName = null) : base(targetKernelName)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(filePath));
        }

        FilePath = filePath;
    }

    public string FilePath { get; }

    internal static Task<KernelCommand> TryParseImportDirectiveAsync(
        DirectiveNode directiveNode,
        ExpressionBindingResult bindingResult,
        Kernel kernel)
    {
        ImportDocument command = null;

        if (directiveNode.TryGetActionDirective(out var directive))
        {
            var parameterValues = directiveNode.GetParameterValues(directive, bindingResult.BoundValues).ToArray();

            var parameterResult = parameterValues.SingleOrDefault(v => v.Name is "");

            if (parameterResult.Value is string filePath)
            {
                {
                    command = new ImportDocument(filePath);
                }
            }
        }

        return Task.FromResult<KernelCommand>(command);
    }
}