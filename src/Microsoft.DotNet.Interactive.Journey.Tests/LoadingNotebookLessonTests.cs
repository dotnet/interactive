// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Journey.Tests.Utilities;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Journey.Tests;

public class LoadingNotebookLessonTests : ProgressiveLearningTestBase
{
    [Fact]
    public async Task teacher_can_load_notebook_from_url()
    {
        var capturedCommands = new List<SendEditableCode>();
        var client = new HttpClient(new FakeHttpMessageHandlerForNotebookLoading());
        var kernel = await CreateKernel(LessonMode.StudentMode, client);
        var vscodeKernel = kernel.FindKernelByName("vscode");
        vscodeKernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            capturedCommands.Add(command);
            return Task.CompletedTask;
        });
        using var events = kernel.KernelEvents.ToSubscribedList();
        await kernel.SubmitCodeAsync(@"#!start-lesson --from-url ""http://wat.com/twoChallenges.dib""");

        await kernel.SubmitCodeAsync("Console.WriteLine(1 + 1);");

        capturedCommands.GetRange(2, 2).Should().SatisfyRespectively(
            e => e.Code.Should().Contain("// write your answer to DFS below"),
            e => e.Code.Should().Contain("This is the DFS question."));
    }

    [Fact]
    public async Task teacher_can_run_lesson_setup_code()
    {
        var kernel = await CreateKernel(LessonMode.StudentMode);
        using var events = kernel.KernelEvents.ToSubscribedList();
        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("twoChallenges.dib")}");

        await kernel.SubmitCodeAsync("lessonSetupVar");

        events.Should().ContainSingle<ReturnValueProduced>().Which.Value.Should().Be(666);
    }

    [Fact]
    public async Task teacher_can_run_lesson_setup_code_in_the_same_cell_as_package_import()
    {
        var kernel = await CreateKernel(LessonMode.StudentMode);
        using var events = kernel.KernelEvents.ToSubscribedList();
        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("twoChallenges.dib")}");

        await kernel.SubmitCodeAsync("lessonSetupVarInTheFirstCell");

        events.Should().ContainSingle<ReturnValueProduced>().Which.Value.Should().Be(333);
    }

    [Fact]
    public async Task teacher_can_use_add_rule_when_starting_a_lesson()
    {
        var kernel = await CreateKernel(LessonMode.StudentMode);

        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("singleChallenge.dib")}");

        var result = await kernel.SubmitCodeAsync("1");

        result.Events
              .Should()
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == HtmlFormatter.MimeType)
              .Which
              .Value
              .Should()
              .ContainAll(
                  "failrule",
                  "fail reasons",
                  "passrule",
                  "pass reasons");
    }

    [Fact]
    public async Task teacher_can_use_on_code_submitted_when_starting_a_lesson()
    {
        var kernel = await CreateKernel(LessonMode.StudentMode);
        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("singleChallenge.dib")}");

        var result = await kernel.SubmitCodeAsync("1");

        result.Events
              .Should()
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == HtmlFormatter.MimeType)
              .Which
              .Value
              .Should()
              .Contain("Good job");
    }

    [Fact]
    public async Task teacher_can_run_challenge_environment_setup_code_when_starting_a_lesson()
    {
        var kernel = await CreateKernel(LessonMode.StudentMode);
        using var events = kernel.KernelEvents.ToSubscribedList();
        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("singleChallenge.dib")}");

        await kernel.SubmitCodeAsync("challengeSetupVar");

        events.Should().ContainSingle<ReturnValueProduced>().Which.Value.Should().Be(3);
    }

    [Fact]
    public async Task teacher_can_show_challenge_contents_when_starting_a_lesson()
    {
        var capturedCommands = new List<SendEditableCode>();
        var kernel = await CreateKernel(LessonMode.StudentMode);
        var vscodeKernel = kernel.FindKernelByName("vscode");
        vscodeKernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            capturedCommands.Add(command);
            return Task.CompletedTask;
        });
        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("singleChallenge.dib")}");

        capturedCommands.Should().SatisfyRespectively(
            e => e.Code.Should().Contain("This is the LinkedList question."),
            e => e.Code.Should().Contain("// write your answer to LinkedList question below"));
    }

    [Fact]
    public async Task when_starting_a_lesson_the_shown_challenge_contents_do_not_contain_directives()
    {
        var capturedCommands = new List<SendEditableCode>();
        var kernel = await CreateKernel(LessonMode.StudentMode);
        var vscodeKernel = kernel.FindKernelByName("vscode");
        vscodeKernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            capturedCommands.Add(command);
            return Task.CompletedTask;
        });
        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("singleChallenge.dib")}");

        capturedCommands.Select(c => c.Code).Join("\r\n")
            .Should().NotContainAny(NotebookLessonParser.AllDirectiveNames);
    }

    [Fact]
    public async Task when_starting_a_lesson_the_shown_challenge_contents_do_not_contain_scratchpad_material()
    {
        var capturedCommands = new List<SendEditableCode>();
        var kernel = await CreateKernel(LessonMode.StudentMode);
        var vscodeKernel = kernel.FindKernelByName("vscode");
        vscodeKernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            capturedCommands.Add(command);
            return Task.CompletedTask;
        });
        using var events = kernel.KernelEvents.ToSubscribedList();

        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("singleChallenge.dib")}");

        capturedCommands.Select(c => c.Code).Join("\r\n")
            .Should().NotContainAny("// random scratchpad stuff");
    }

    [Fact]
    public async Task teacher_can_use_add_rule_when_progressing_the_student_to_different_challenge()
    {
        var kernel = await CreateKernel(LessonMode.StudentMode);
        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("twoChallenges.dib")}");
        await kernel.SubmitCodeAsync("Console.WriteLine(1 + 1);");

        var result = await kernel.SubmitCodeAsync("1 + 1");

        result.Events.Should()
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == HtmlFormatter.MimeType)
              .Which
              .Value
              .Should()
              .ContainAll(
                  "dfsrule1",
                  "dfsfailreasons",
                  "dfsrule2",
                  "dfspassreasons");
    }

    [Fact]
    public async Task teacher_can_use_on_code_submitted_when_progressing_the_student_to_different_challenge()
    {
        var kernel = await CreateKernel(LessonMode.StudentMode);
        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("twoChallenges.dib")}");
        await kernel.SubmitCodeAsync("Console.WriteLine(1 + 1);");

        var result = await kernel.SubmitCodeAsync("1 + 1");

        result.Events.Should()
              .ContainSingle<DisplayedValueProduced>()
              .Which
              .FormattedValues
              .Should()
              .ContainSingle(v => v.MimeType == HtmlFormatter.MimeType)
              .Which
              .Value
              .Should()
              .Contain(
                  "Good job for DFS");
    }

    [Fact]
    public async Task teacher_can_run_challenge_environment_setup_code_when_progressing_the_student_to_different_challenge()
    {
        var kernel = await CreateKernel(LessonMode.StudentMode);
        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("twoChallenges.dib")}");
        await kernel.SubmitCodeAsync("Console.WriteLine(1 + 1);");

        var result = await kernel.SubmitCodeAsync("anotherChallengeSetupVar");

        result.Events.Should().ContainSingle<ReturnValueProduced>().Which.Value.Should().Be(10);
    }

    [Fact]
    public async Task teacher_can_show_challenge_contents_when_progressing_the_student_to_different_challenge()
    {
        var capturedCommands = new List<SendEditableCode>();
        var kernel = await CreateKernel(LessonMode.StudentMode);
        var vscodeKernel = kernel.FindKernelByName("vscode");
        vscodeKernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            capturedCommands.Add(command);
            return Task.CompletedTask;
        });
        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("twoChallenges.dib")}");

        await kernel.SubmitCodeAsync("Console.WriteLine(1 + 1);");

        capturedCommands.GetRange(2, 2).Should().SatisfyRespectively(
            e => e.Code.Should().Contain("// write your answer to DFS below"),
            e => e.Code.Should().Contain("This is the DFS question."));
    }

    [Fact]
    public async Task when_progressing_the_student_to_different_challenge_the_shown_challenge_contents_do_not_contain_directives()
    {
        var capturedCommands = new List<SendEditableCode>();
        var kernel = await CreateKernel(LessonMode.StudentMode);
        var vscodeKernel = kernel.FindKernelByName("vscode");
        vscodeKernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            capturedCommands.Add(command);
            return Task.CompletedTask;
        });
        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("twoChallenges.dib")}");

        await kernel.SubmitCodeAsync("Console.WriteLine(1 + 1);");

        capturedCommands.Select(c => c.Code).Join("\r\n")
            .Should()
            .NotContainAny(NotebookLessonParser.AllDirectiveNames);
    }

    [Fact]
    public async Task
        when_progressing_the_student_to_different_challenge_the_shown_challenge_contents_do_not_contain_scratchpad_material()
    {
        var capturedCommands = new List<SendEditableCode>();
        var kernel = await CreateKernel(LessonMode.StudentMode);
        var vscodeKernel = kernel.FindKernelByName("vscode");
        vscodeKernel.RegisterCommandHandler<SendEditableCode>((command, _) =>
        {
            capturedCommands.Add(command);
            return Task.CompletedTask;
        });
        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("singleChallenge.dib")}");

        await kernel.SubmitCodeAsync("Console.WriteLine(1 + 1);");

        capturedCommands.Select(c => c.Code).Join("\r\n")
            .Should()
            .NotContainAny("// random scratchpad stuff");
    }

    [Fact]
    public async Task teacher_can_declare_identifiers_and_let_it_become_replaced_by_the_students_answer()
    {
        var kernel = await CreateKernel(LessonMode.StudentMode);
        await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("variableReplacing.dib")}");

        var result = await kernel.SubmitCodeAsync(@"
CalcTrigArea = (double x, double y) => 0.5 * x * y;
");

        result.Events
              .Should()
              .ContainSingle<DisplayedValueProduced>(
                  e => e.FormattedValues.Single(v => v.MimeType == "text/html")
                        .Value.Contains("You passed"));
    }

    [Fact]
    public async Task for_start_lesson_command_from_url_and_from_file_options_cannot_be_used_together()
    {
        var kernel = await CreateKernel(LessonMode.StudentMode);
            
        var result = await kernel.SubmitCodeAsync($"#!start-lesson --from-file {GetPatchedNotebookPath("twoChallenges.dib")} --from-url http://bing.com");
            
        result.Events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .Be("The --from-url and --from-file options cannot be used together");
    }
}