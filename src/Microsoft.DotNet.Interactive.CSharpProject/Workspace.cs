// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.CSharpProject;

public class Workspace
{
    private const string DefaultWorkspaceType = "script";
    private const string DefaultLanguage = "csharp";

    public Workspace(
        string[] usings = null,
        ProjectFileContent[] files = null,
        Buffer[] buffers = null,
        string workspaceType = DefaultWorkspaceType,
        string language =  DefaultLanguage)
    {
        WorkspaceType = string.IsNullOrWhiteSpace(workspaceType) ? DefaultWorkspaceType : workspaceType;
        Language = string.IsNullOrWhiteSpace(language) ? DefaultLanguage : language;
        Usings = usings ?? Array.Empty<string>();
        Usings = usings ?? Array.Empty<string>();
        Files = files ?? Array.Empty<ProjectFileContent>();
        Buffers = buffers ?? Array.Empty<Buffer>();

        if (Files.Distinct().Count() != Files.Length )
        {
            throw new ArgumentException($"Duplicate file names:{Environment.NewLine}{string.Join(Environment.NewLine, Files.Select(f => f.Name))}");
        }
            
        if (Buffers.Distinct().Count() != Buffers.Length )
        {
            throw new ArgumentException($"Duplicate buffer ids:{Environment.NewLine}{string.Join(Environment.NewLine, Buffers.Select(b => b.Id))}");
        }
    }

    [JsonProperty("language")]
    public string Language { get; }

    [JsonProperty("files")]
    public ProjectFileContent[] Files { get; }

    [JsonProperty("usings")]
    public string[] Usings { get; }

    [JsonProperty("workspaceType")]
    public string WorkspaceType { get; }

    [Required]
    [MinLength(1)]
    [JsonProperty("buffers")]
    public Buffer[] Buffers { get; }

    public static Workspace FromSource(
        string source,
        string workspaceType,
        string id = "Program.cs",
        string[] usings = null,
        string language = DefaultLanguage,
        int position = 0)
    {
        return new Workspace(
            workspaceType: workspaceType,
            language: language,
            buffers: new[]
            {
                new Buffer(BufferId.Parse(id ?? throw new ArgumentNullException(nameof(id))), source, position)
            },
            usings: usings);
    }

    public static Workspace FromDirectory(
        DirectoryInfo directory,
        string workspaceType,
        bool includeInstrumentation = false)
    {
        var filesOnDisk = directory.GetFiles("*.cs", SearchOption.AllDirectories)
                                   .Where(f => !f.IsBuildOutput())
                                   .ToArray();

        var files = filesOnDisk.Select(file => new ProjectFileContent(file.Name, file.Read())).ToList();

        return new Workspace(
            files: files.ToArray(),
            buffers: files.Select(f => new Buffer(
                                      f.Name,
                                      filesOnDisk.Single(fod => fod.Name == f.Name)
                                                 .Read()))
                          .ToArray(),
            workspaceType: workspaceType);
    }
}