// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class ProjectItem
{
    private readonly RelativeFilePath _relativeFilePath;
    public string RelativeFilePath => _relativeFilePath.ToString();

    public IReadOnlyList<string> RegionNames { get; }
    public IReadOnlyDictionary<string, string> RegionsContent { get; }

    public ProjectItem(RelativeFilePath relativeFilePath, IReadOnlyList<string> regionNames, IReadOnlyDictionary<string,string> regionsContent)
    {
        _relativeFilePath = relativeFilePath;
        RegionNames = regionNames;
        RegionsContent = regionsContent;
    }

    [JsonConstructor]
    public ProjectItem(string relativeFilePath, IReadOnlyList<string> regionNames, IReadOnlyDictionary<string, string> regionsContent)
        : this(new RelativeFilePath(relativeFilePath), regionNames, regionsContent)
    {
    }
}