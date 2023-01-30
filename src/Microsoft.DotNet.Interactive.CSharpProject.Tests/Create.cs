// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Concurrency;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Recipes;
using Microsoft.DotNet.Interactive.CSharpProject.Packaging;
using Package = Microsoft.DotNet.Interactive.CSharpProject.Packaging.Package;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests;

public static class Create
{
    public static async Task<Package> ConsoleWorkspaceCopy([CallerMemberName] string testName = null, bool isRebuildable = false, IScheduler buildThrottleScheduler = null) =>
        await PackageUtilities.Copy(
            await Default.ConsoleWorkspaceAsync(),
            testName,
            isRebuildable,
            buildThrottleScheduler);

    public static Package EmptyWorkspace([CallerMemberName] string testName = null, IPackageInitializer initializer = null, bool isRebuildablePackage = false)
    {
        if (!isRebuildablePackage)
        {
            return new NonrebuildablePackage(directory: PackageUtilities.CreateDirectory(testName), initializer: initializer);
        }

        return new RebuildablePackage(directory: PackageUtilities.CreateDirectory(testName), initializer: initializer);
    }

    public static string SimpleWorkspaceRequestAsJson(
        string consoleOutput = "Hello!",
        string workspaceType = null,
        string workspaceLanguage = "csharp")
    {
        var workspace = Workspace.FromSource(
            SimpleConsoleAppCodeWithoutNamespaces(consoleOutput),
            workspaceType,
            "Program.cs"
        );

        return new WorkspaceRequest(workspace, requestId: "TestRun").ToJson();
    }

    public static string SimpleConsoleAppCodeWithoutNamespaces(string consoleOutput)
    {
        var code = $@"
using System;

public static class Hello
{{
    public static void Main()
    {{
        Console.WriteLine(""{consoleOutput}"");
    }}
}}";
        return code.EnforceLF();
    }
}