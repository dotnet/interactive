// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Parsing;

namespace Microsoft.DotNet.Interactive.Commands;

public class SubmitCode : KernelCommand
{
    public SubmitCode(
        string code,
        string targetKernelName = null) : base(targetKernelName)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }
      
    internal SubmitCode(
        LanguageNode languageNode,
        KernelNameDirectiveNode kernelNameDirectiveNode = null)
        : base(languageNode.Name)
    {
        Code = languageNode.Text;
        LanguageNode = languageNode;
        KernelNameDirectiveNode = kernelNameDirectiveNode;
        SchedulingScope = SchedulingScope.Parse(languageNode.CommandScope);

        if (languageNode is ActionDirectiveNode actionDirectiveNode)
        {
            TargetKernelName = actionDirectiveNode.ParentKernelName;
        }
    }

    public string Code { get; internal set; }

    public override string ToString() => $"{nameof(SubmitCode)}: {Code?.TruncateForDisplay()}";

    internal LanguageNode LanguageNode { get; }

    internal KernelNameDirectiveNode KernelNameDirectiveNode { get; }
}