// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.CSharpProject.Protocol;

namespace Microsoft.DotNet.Interactive.CSharpProject.MLS.Project
{
    public static class FileExtensions
    {
        public static SourceFile ToSourceFile(this File file)
        {
            return SourceFile.Create(file.Text, file.Name);
        }

        public static IEnumerable<Viewport> ExtractViewPorts(this File file)
        {
            return file.ToSourceFile().ExtractViewPorts();
        }

        public static IEnumerable<Viewport> ExtractViewPorts(this SourceFile sourceFile)
        {
            var code = sourceFile.Text;
            var fileName = sourceFile.Name;
            var regions = code.ExtractRegions(fileName);

            var seenBuffers = new HashSet<string>();
            foreach (var region in regions)
            {
                if (!seenBuffers.Add(region.bufferId.ToString()))
                {
                    throw new InvalidOperationException("viewport identifiers must be unique");
                }

                yield return new Viewport(sourceFile, region.span, region.outerSpan, region.bufferId);
            }
        }

        public static IEnumerable<Viewport> ExtractViewports(this IEnumerable<File> files)
        {
            return files.Select(f => f.ToSourceFile()).ExtractViewports();
        }

        public static IEnumerable<Viewport> ExtractViewports(this IEnumerable<SourceFile> sourceFiles)
        {
            return sourceFiles.SelectMany(f => f.ExtractViewPorts());
        }
    }
}