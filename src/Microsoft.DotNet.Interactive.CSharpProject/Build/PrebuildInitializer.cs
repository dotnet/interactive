// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.CSharpProject.Build;

public class PrebuildInitializer : IPrebuildInitializer
{
    private readonly Func<DirectoryInfo, Task> afterCreate;

    public PrebuildInitializer(
        string template,
        string projectName,
        string language = null,
        Func<DirectoryInfo, Task> afterCreate = null)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(template));
        }

        if (string.IsNullOrWhiteSpace(projectName))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(projectName));
        }

        this.afterCreate = afterCreate;

        Template = template;
        ProjectName = projectName;
        Language = language ?? GetLanguageFromProjectName(ProjectName);
    }

    public string Template { get; }

    public string Language { get; }

    public string ProjectName { get; }

    public virtual async Task InitializeAsync(DirectoryInfo directory)
    {
        var dotnet = new Dotnet(directory);

        await dotnet.New("globaljson", "--force");

        var result = await dotnet
            .New(Template,
                args: $"--name \"{ProjectName}\" --language \"{Language}\" --output \"{directory.FullName}\" --force");
        result.ThrowOnFailure($"Error initializing in {directory.FullName}");

        if (afterCreate is not null)
        {
            await afterCreate(directory);
        }
    }

    private static string GetLanguageFromProjectName(string projectName)
    {
        if (projectName.EndsWith(".fsproj", StringComparison.OrdinalIgnoreCase))
        {
            return "F#";
        }

        // default to C#
        return "C#";
    }
}