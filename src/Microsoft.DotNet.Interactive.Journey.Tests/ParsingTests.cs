// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Journey.Tests.Utilities;
using Xunit;

namespace Microsoft.DotNet.Interactive.Journey.Tests;

public class ParsingTests : ProgressiveLearningTestBase
{
    [Fact]
    public void parser_can_parse_teacher_notebook_with_two_challenges_with_all_components_defined()
    {
        InteractiveDocument document = ReadDib("forParsing1.dib");

        NotebookLessonParser.Parse(document, out var lesson, out var challenges);

        lesson.Setup.Select(sc => sc.Code).Join("\r\n").Should().ContainAll("lessonSetupCell1", "lessonSetupCell2");

        challenges[0].Name.Should().Be("Challenge1Name");

        challenges[0].Setup.Select(sc => sc.Code).Join("\r\n")
            .Should().ContainAll("challenge1SetupCell1", "challenge1SetupCell2");

        challenges[0].EnvironmentSetup.Select(sc => sc.Code).Join("\r\n")
            .Should().ContainAll("challenge1EnvironmentSetupCell1", "challenge1EnvironmentSetupCell2");

        challenges[0].Contents.Select(sec => sec.Code).Join("\r\n")
            .Should().ContainAll("challenge1QuestionCell1", "challenge1QuestionCell2");

        challenges[1].Name.Should().Be("Challenge2Name");

        challenges[1].Setup.Select(sc => sc.Code).Join("\r\n")
            .Should().ContainAll("challenge2SetupCell1", "challenge2SetupCell2");

        challenges[1].EnvironmentSetup.Select(sc => sc.Code).Join("\r\n")
            .Should().ContainAll("challenge2EnvironmentSetupCell1", "challenge2EnvironmentSetupCell2");

        challenges[1].Contents.Select(sec => sec.Code).Join("\r\n")
            .Should().ContainAll("challenge2QuestionCell1", "challenge2QuestionCell2");
    }

    [Fact]
    public void duplicate_challenge_name_causes_parser_to_throw_exception()
    {
        InteractiveDocument document = ReadDib("forParsing2DuplicateChallengeName.dib");

        Action parsingDuplicateChallengeName = () => NotebookLessonParser.Parse(document, out var _, out var _);

        parsingDuplicateChallengeName
            .Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("conflicts");
    }

    [Fact]
    public void notebook_with_no_challenge_causes_parser_to_throw_exception()
    {
        InteractiveDocument document = ReadDib("noChallenge.dib");

        Action parsingDuplicateChallengeName = () => NotebookLessonParser.Parse(document, out var _, out var _);

        parsingDuplicateChallengeName
            .Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("This lesson has no challenges");
    }

    [Fact]
    public void a_challenge_with_no_question_causes_parser_to_throw_exception()
    {
        InteractiveDocument document = ReadDib("challengeWithNoQuestion.dib");

        Action parsingDuplicateChallengeName = () => NotebookLessonParser.Parse(document, out var _, out var _);

        parsingDuplicateChallengeName
            .Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("empty question");
    }

    private InteractiveDocument ReadDib(string notebookName)
    {
        return NotebookLessonParser.ReadFileAsInteractiveDocument(new FileInfo(GetPatchedNotebookPath(notebookName)));
    }
}