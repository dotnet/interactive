// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Directives;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive.Journey;

public static class KernelExtensions
{
    private static readonly string _modelAnswerCommandName = "#!model-answer";

    public static CompositeKernel UseModelAnswerValidation(this CompositeKernel kernel)
    {
        var modelAnswerCommand = new KernelActionDirective(_modelAnswerCommandName);
        kernel.AddDirective(modelAnswerCommand, (_, _) => Task.CompletedTask);
        return kernel;
    }

    public static CompositeKernel UseProgressiveLearning(
        this CompositeKernel kernel,
        HttpClient httpClient = null)
    {
        kernel.Bootstrapping();

        var fromUrlOption = new KernelDirectiveParameter("--from-url")
        {
            Description = LocalizationResources.Magics_model_answer_from_url_Description()
        };

        var fromFileOption = new KernelDirectiveParameter("--from-file")
        {
            Description = LocalizationResources.Magics_model_answer_from_file_Description()
        };

        var directive = new KernelActionDirective("#!start-lesson");
        directive.Parameters.Add(fromFileOption);
        directive.Parameters.Add(fromUrlOption);

        kernel.AddDirective<StartLesson>(directive, StartCommandHandler);

        return kernel;

        async Task StartCommandHandler(StartLesson command, KernelInvocationContext context)
        {
            var document = command.FromFile switch
            {
                not null => NotebookLessonParser.ReadFileAsInteractiveDocument(command.FromFile, kernel),
                _ => await NotebookLessonParser.LoadNotebookFromUrl(command.FromUrl, httpClient)
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