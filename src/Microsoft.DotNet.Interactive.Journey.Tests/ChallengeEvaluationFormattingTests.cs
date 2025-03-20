// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using HtmlAgilityPack;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.Journey.Tests;

[TestClass]
public class ChallengeEvaluationFormattingTests
{
    [TestMethod]
    public void teacher_can_provide_feedback_for_a_specific_rule()
    {
        // arrange
        var evaluation = new ChallengeEvaluation();
        evaluation.SetRuleOutcome("Code compiles", Outcome.Success, "Your submission has compiled.");

        // act
        var message = evaluation.ToDisplayString(HtmlFormatter.MimeType);
        // assert
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(message);

        var summary = htmlDoc.DocumentNode
            .SelectSingleNode("//details[@class='ruleEvaluation']/summary");

        summary.InnerText
            .Should()
            .Be("[ Code compiles ]: Success");

        var p = htmlDoc.DocumentNode
            .SelectSingleNode("//details[@class='ruleEvaluation']/div");

        p.InnerText
            .Should()
            .Be("Your submission has compiled.");
    }

    [TestMethod]
    public void display_number_of_rules()
    {
        // arrange
        var evaluation = new ChallengeEvaluation();
        evaluation.SetRuleOutcome("Code compiles", Outcome.Success);
        evaluation.SetRuleOutcome("Code matches output", Outcome.Success);
        evaluation.SetRuleOutcome("Code is recursive", Outcome.Failure);

        // act
        var message = evaluation.ToDisplayString(HtmlFormatter.MimeType);
        // assert
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(message);

        var summary = htmlDoc.DocumentNode
            .SelectSingleNode("//details[@class='challengeEvaluation']/summary");

        summary.InnerText
            .Should()
            .Be("(2/3) rules have passed.");
    }
}