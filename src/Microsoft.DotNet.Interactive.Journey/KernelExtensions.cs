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
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Journey
{
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
                "Specify lesson source URL");

            var fromFileOption = new Option<FileInfo>(
                "--from-file",
                description: "Specify lesson source file",
                parseArgument: result =>
                {
                    var filePath = result.Tokens.Single().Value;
                    var fromUrlResult = result.FindResultFor(fromUrlOption);
                    if (fromUrlResult is not null)
                    {
                        result.ErrorMessage = $"The {fromUrlResult.Token.Value} and {(result.Parent as OptionResult).Token.Value} options cannot be used together";
                        return null;
                    }

                    if (!File.Exists(filePath))
                    {
                        result.ErrorMessage = LocalizationResources.Instance.FileDoesNotExist(filePath);
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
                
                InteractiveDocument document = fromFile switch
                {
                    { } => await NotebookLessonParser.ReadFileAsInteractiveDocument(fromFile, kernel),
                    _ => await NotebookLessonParser.LoadNotebookFromUrl(fromUrl, httpClient)
                };

                NotebookLessonParser.Parse(document, out var lessonDefinition, out var challengeDefinitions);

                var challenges = challengeDefinitions.Select(b => b.ToChallenge()).ToList();
                challenges.SetDefaultProgressionHandlers();
                Lesson.From(lessonDefinition);
                Lesson.SetChallengeLookup(queryName => challenges.FirstOrDefault(c => c.Name == queryName));

                await kernel.StartLesson();

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

                        List<KernelEvent> events = new();

                        using (context.KernelEvents.Subscribe(events.Add))
                        {
                            await next(command, context);
                        }

                        if (Lesson.CurrentChallenge is { })
                        {
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
                foreach (var setup in challengeToInit.Setup)
                {
                    await kernel.SendAsync(setup);
                }

                challengeToInit.IsSetup = true;
            }

            foreach (var content in challengeToInit.Contents)
            {
                await kernel.SendAsync(content);
            }

            foreach (var setup in challengeToInit.EnvironmentSetup)
            {
                await kernel.SendAsync(setup);
            }
        }

        private static void Bootstrapping(this Kernel kernel)
        {
            if (kernel.RootKernel.FindKernel("csharp") is CSharpKernel csharpKernel)
            {
                csharpKernel.DeferCommand(new SubmitCode($"#r \"{typeof(Lesson).Assembly.Location}\"", csharpKernel.Name));
                csharpKernel.DeferCommand(new SubmitCode($"using {typeof(Lesson).Namespace};", csharpKernel.Name));
            }
        }

        private static async Task StartLesson(this Kernel kernel)
        {
            Lesson.Mode = LessonMode.StudentMode;
            foreach (var setup in Lesson.Setup)
            {
                await kernel.SendAsync(setup);
            }
        }
    }
}