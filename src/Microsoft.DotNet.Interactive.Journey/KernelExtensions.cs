// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Journey;

public static class KernelExtensions
{
    private static readonly string _modelAnswerCommandName = "#!model-answer";

    public static CompositeKernel UseModelAnswerValidation(this CompositeKernel kernel)
    {
        var modelAnswerCommand = new Command(_modelAnswerCommandName);
        kernel.AddDirective(modelAnswerCommand);
        return kernel;
    }

    public static CompositeKernel UseProgressiveLearning(
        this CompositeKernel kernel,
        HttpClient httpClient = null)
    {
        kernel.Bootstrapping();

        var fromUrlOption = new Option<Uri>(
            "--from-url",
            LocalizationResources.Magics_model_answer_from_url_Description());

        var fromFileOption = new Option<FileInfo>(
            "--from-file",
            description: LocalizationResources.Magics_model_answer_from_file_Description(),
            parseArgument: result =>
            {
                var filePath = result.Tokens.Single().Value;
                var fromUrlResult = result.FindResultFor(fromUrlOption);
                if (fromUrlResult is not null)
                {
                    result.ErrorMessage = LocalizationResources.Magics_model_answer_from_file_ErrorMessage(fromUrlResult.Token.Value, (result.Parent as OptionResult).Token.Value);
                    return null;
                }

                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = LocalizationResources.FileDoesNotExist(filePath);
                    return null;
                }

                return new FileInfo(filePath);
            });

        var startCommand = new Command("#!start-lesson")
        {
            fromFileOption,
            fromUrlOption
        };

        startCommand.Handler = CommandHandler.Create(StartCommandHandler);

        kernel.AddDirective(startCommand);

        return kernel;

        async Task StartCommandHandler(InvocationContext cmdlLineContext)
        {
            var fromFile = cmdlLineContext.ParseResult.GetValueForOption(fromFileOption);
            var fromUrl = cmdlLineContext.ParseResult.GetValueForOption(fromUrlOption);

            var document = fromFile switch
            {
                { } => NotebookLessonParser.ReadFileAsInteractiveDocument(fromFile, kernel),
                _ => await NotebookLessonParser.LoadNotebookFromUrl(fromUrl, httpClient)
            };

            NotebookLessonParser.Parse(document, out var lessonDefinition, out var challengeDefinitions);

            var challenges = challengeDefinitions.Select(b => b.ToChallenge()).ToList();
            challenges.SetDefaultProgressionHandlers();
            Lesson.From(lessonDefinition);
            Lesson.SetChallengeLookup(queryName => challenges.FirstOrDefault(c => c.Name == queryName));

            Lesson.Mode = LessonMode.StudentMode;
            foreach (var setup in Lesson.Setup)
            {
                await kernel.SendAsync(setup);
            }

            await Lesson.StartChallengeAsync(challenges.First());

            await kernel.InitializeChallenge(Lesson.CurrentChallenge);
        }
    }

    public static CompositeKernel UseProgressiveLearningMiddleware(this CompositeKernel kernel)
    {
        kernel.AddMiddleware(async (command, context, next) =>
        {
            switch (command)
            {
                case SubmitCode submitCode:
                    var isSetupCommand = Lesson.IsSetupCommand(submitCode);
                    var isModelAnswer = submitCode.Parent is SubmitCode submitCodeParent &&
                                        submitCodeParent.Code.TrimStart().StartsWith(_modelAnswerCommandName);

                    if (Lesson.Mode == LessonMode.StudentMode &&
                        isSetupCommand ||
                        Lesson.Mode == LessonMode.TeacherMode &&
                        !isModelAnswer)
                    {
                        await next(command, context);
                        break;
                    }

                    var currentChallenge = Lesson.CurrentChallenge;

                    if (Lesson.CurrentChallenge is { })
                    {
                        List<KernelEvent> events = new();
                        using (context.KernelEvents.Subscribe(events.Add))
                        {
                            await next(command, context);
                        }

                        await Lesson.CurrentChallenge.Evaluate(submitCode.Code, events);

                        context.Display(currentChallenge.CurrentEvaluation);

                        if (Lesson.CurrentChallenge != currentChallenge)
                        {
                            switch (Lesson.Mode)
                            {
                                case LessonMode.StudentMode:
                                    await InitializeChallenge(kernel, Lesson.CurrentChallenge);
                                    break;
                                case LessonMode.TeacherMode:
                                    await Lesson.StartChallengeAsync(currentChallenge);
                                    break;
                            }
                        }
                    }
                    else
                    {
                        await next(command, context);
                    }

                    break;
                default:
                    await next(command, context);
                    break;
            }
        });

        return kernel;
    }

    public static async Task InitializeChallenge(this Kernel kernel, Challenge challengeToInit)
    {
        if (challengeToInit is null)
        {
            return;
        }

        if (!challengeToInit.IsSetup)
        {
            foreach (var setup in challengeToInit.Setup.Select(CreateNewSubmitCodeWithCode))
            {
                await kernel.SendAsync(setup);
            }

            challengeToInit.IsSetup = true;
        }

        foreach (var content in challengeToInit.Contents.Select(CreateNewSendEditableCodeWithContent))
        {
            await kernel.SendAsync(content);
        }

        foreach (var setup in challengeToInit.EnvironmentSetup.Select(CreateNewSubmitCodeWithCode))
        {
            await kernel.SendAsync(setup);
        }
    }

    private static SubmitCode CreateNewSubmitCodeWithCode(SubmitCode original)
    {
        var command = new SubmitCode(original.Code, original.TargetKernelName);
        return command;
    }

    private static SendEditableCode CreateNewSendEditableCodeWithContent(SendEditableCode original)
    {
        var command = new SendEditableCode(original.KernelName, original.Code);
        return command;
    }

    private static void Bootstrapping(this Kernel kernel)
    {
        if (kernel.RootKernel.FindKernelByName("csharp") is CSharpKernel csharpKernel)
        {
            csharpKernel.DeferCommand(new SubmitCode($"#r \"{typeof(Lesson).Assembly.Location}\"", csharpKernel.Name));
            csharpKernel.DeferCommand(new SubmitCode($"using {typeof(Lesson).Namespace};", csharpKernel.Name));
        }
    }
}

