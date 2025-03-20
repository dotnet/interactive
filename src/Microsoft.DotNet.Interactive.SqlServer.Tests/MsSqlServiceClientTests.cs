// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;

namespace Microsoft.DotNet.Interactive.SqlServer.Tests;

[TestClass]
public class MsSqlServiceClientTests
{
    [TestMethod]
    [DataRow("\r\n")]
    [DataRow("\n")]
    public void Should_parse_doc_change_correctly_with_different_line_endings(string lineEnding)
    {
        string oldText = string.Join(lineEnding, "abc", "def", "", "abc", "abcdef");
        int oldTextLineCount = 5;
        int oldTextLastCharacterNum = 6;
        string newText = string.Join(lineEnding, "abc", "def");
        var testUri = new Uri("untitled://test");

        var docChange = ToolsServiceClient.GetDocumentChangeForText(testUri, newText, oldText);

        docChange.ContentChanges.Length
            .Should()
            .Be(1);
        docChange.ContentChanges[0].Range.End.Line
            .Should()
            .Be(oldTextLineCount - 1);
        docChange.ContentChanges[0].Range.End.Character
            .Should()
            .Be(oldTextLastCharacterNum);
        docChange.ContentChanges[0].Range.Start.Line
            .Should()
            .Be(0);
        docChange.ContentChanges[0].Range.Start.Character
            .Should()
            .Be(0);
        docChange.ContentChanges[0].Text
            .Should()
            .Be(newText);
        docChange.TextDocument.Uri
            .Should()
            .Be(testUri.AbsolutePath);
        docChange.TextDocument.Version
            .Should()
            .Be(1);
    }
}