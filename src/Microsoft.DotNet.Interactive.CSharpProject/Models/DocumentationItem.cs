// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.CSharpProject.Models;

public class DocumentationItem
{
    public string Name { get; }
    public string Documentation { get; }
    public DocumentationItem(string name, string documentation)
    {
        Name = name;
        Documentation = documentation;
    }
}