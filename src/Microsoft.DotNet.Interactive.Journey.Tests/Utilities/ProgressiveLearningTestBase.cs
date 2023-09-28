// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.App;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Journey.Tests.Utilities;

public abstract class ProgressiveLearningTestBase
{
    private string? _tempPath;

    protected Challenge GetEmptyChallenge()
    {
        return new Challenge();
    }

    protected async Task<CompositeKernel> CreateKernel(LessonMode mode, HttpClient? httpClient = null)
    {
        var vscodeKernel = new FakeKernel("vscode");
        vscodeKernel.RegisterCommandHandler<SendEditableCode>((_, _) => Task.CompletedTask);
        var compositeKernel = new CompositeKernel
        {
            new CSharpKernel().UseNugetDirective().UseKernelHelpers(),
            vscodeKernel
        };

        compositeKernel.DefaultKernelName = "csharp";
        compositeKernel.SetDefaultTargetKernelNameForCommand(typeof(SendEditableCode), "vscode");

        Lesson.Mode = mode;

        await Main.OnLoadAsync(compositeKernel, httpClient);

        return compositeKernel;
    }

    protected string ToModelAnswer(string answer)
    {
        return $"#!model-answer\r\n{answer}";
    }

    protected string GetPatchedNotebookPath(string notebookName, [CallerMemberName]string? testName = null)
    {
        var original = PathUtilities.GetNotebookPath(notebookName);
        var txt = File.ReadAllText(original);

        var newNotebookContent = txt.Replace("{journey_dll_path}", $"\"{typeof(KernelExtensions).Assembly.Location}\"");

        _tempPath = Path.Combine( Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempPath);

        var newFilePath = Path.Combine(_tempPath, $"{testName??"patched"}_{notebookName}");

        File.WriteAllText(newFilePath, newNotebookContent);

        return newFilePath;
    }
}