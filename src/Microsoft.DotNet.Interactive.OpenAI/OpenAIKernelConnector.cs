// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.SemanticKernel.SkillDefinition;

namespace Microsoft.DotNet.Interactive.OpenAI;

public class OpenAIKernelConnector
{
    static OpenAIKernelConnector()
    {
        Formatter.Register<FunctionView>((value, context) =>
        {
            context.Writer.Write($"params: {string.Join(", ", value.Parameters.Select(p => p.Name))}");

            return true;
        }, PlainTextSummaryFormatter.MimeType);
    }

    public static void AddKernelConnectorToCurrentRootKernel()
    {
        if (KernelInvocationContext.Current is { } context &&
            context.HandlingKernel.RootKernel is CompositeKernel root)
        {
            AddKernelConnectorTo(root);
        }
    }

    public static void AddKernelConnectorTo(CompositeKernel kernel)
    {
        kernel.AddKernelConnector(new ConnectOpenAICommand());
    }

    public static readonly string SettingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".net-interactive", "OpenAI");
}