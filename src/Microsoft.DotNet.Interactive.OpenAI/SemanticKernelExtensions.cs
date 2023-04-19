// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.SemanticKernel;

namespace Microsoft.DotNet.Interactive.OpenAI;

internal static class SemanticKernelExtensions
{
    public static IEnumerable<string> GetFunctionNames(this IKernel kernel)
    {
        var functionsView = kernel.Skills.GetFunctionsView();

        foreach (var functionView in functionsView
                                     .SemanticFunctions.Concat(functionsView.NativeFunctions)
                                     .SelectMany(p => p.Value))
        {
            yield return $"function.{functionView.SkillName}.{functionView.Name}";
        }
    }
}