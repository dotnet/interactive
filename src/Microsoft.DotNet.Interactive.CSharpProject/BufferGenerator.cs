// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.CSharpProject.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public static class BufferGenerator
{
    public static IEnumerable<Buffer> CreateBuffers(ProjectFileContent content)
    {
        var viewPorts = content.ExtractViewPorts().ToList();
        if (viewPorts.Count > 0)
        {
            foreach (var viewport in viewPorts)
            {
                yield return CreateBuffer(viewport.Region.ToString(), viewport.BufferId);
            }
        }
        else
        {
            yield return CreateBuffer(content.Text, content.Name);
        }
    }

    public static Buffer CreateBuffer(string content, BufferId id)
    {
        MarkupTestFile.GetPosition(content, out var output, out var position);

        return new Buffer(
            id,
            output,
            position ?? 0);
    }
}