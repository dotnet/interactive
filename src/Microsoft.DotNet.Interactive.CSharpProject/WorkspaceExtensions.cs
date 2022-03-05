// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;

namespace Microsoft.DotNet.Interactive.CSharpProject.MLS.Project
{
    public static class WorkspaceExtensions
    {
        public static IReadOnlyCollection<SourceFile> GetSourceFiles(this Workspace workspace)
        {
            return workspace.Files?.Select(f => f.ToSourceFile()).ToArray() ?? Array.Empty<SourceFile>();
        }

        public static IEnumerable<Viewport> ExtractViewPorts(this Workspace ws)
        {
            if (ws == null)
            {
                throw new ArgumentNullException(nameof(ws));
            }

            foreach (var file in ws.Files)
            {
                foreach (var viewPort in file.ExtractViewPorts())
                {
                    yield return viewPort;
                }
            }
        }

        public static Task<Workspace> MergeAsync(this Workspace workspace) => CodeMergeTransformer.Instance.TransformAsync(workspace);

        public static Task<Workspace> InlineBuffersAsync(this Workspace workspace) => BufferInliningTransformer.Instance.TransformAsync(workspace);

        public static async Task<Workspace> InlineBuffersAsync(this Task<Workspace> workspace) => await BufferInliningTransformer.Instance.TransformAsync(await workspace);
    }
}
