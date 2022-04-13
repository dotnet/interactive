// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class ProjectFileContent
{
    public ProjectFileContent(string name, string text, int order = 0)
    {
        Name = name;
        Text = text;
        Order = order;
    }

    public string Name { get; }

    public string Text { get; }

    public int Order { get; }

    public override string ToString() => $"{nameof(ProjectFileContent)}: {Name}";
}