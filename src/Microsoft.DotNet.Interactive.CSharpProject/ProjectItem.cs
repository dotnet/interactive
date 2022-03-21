// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.DotNet.Interactive.CSharpProject.Tools;

namespace Microsoft.DotNet.Interactive.CSharpProject
{
    public class ProjectItem
    {
        private RelativeFilePath _relativeFilePath;
        public string RelativeFilePath => _relativeFilePath.ToString();

        public IReadOnlyList<string> RegionNames { get; }

        public ProjectItem(RelativeFilePath relativeFilePath, IReadOnlyList<string> regionNames)
        {
            _relativeFilePath = relativeFilePath;
            RegionNames = regionNames;
        }

        [JsonConstructor]
        public ProjectItem(string relativeFilePath, IReadOnlyList<string> regionNames)
            : this(new RelativeFilePath(relativeFilePath), regionNames)
        {
        }
    }
}
