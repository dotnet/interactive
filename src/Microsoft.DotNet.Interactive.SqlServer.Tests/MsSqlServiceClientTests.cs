// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;
using FluentAssertions;
using System.IO;

namespace Microsoft.DotNet.Interactive.SqlServer.Tests
{
    public class MsSqlServiceClientTests
    {
        [Fact]
        public void Should_parse_doc_change_correctly_with_windows_line_endings()
        {
            RunDocumentChangeTest("\r\n");
        }

        [Fact]
        public void Should_parse_doc_change_correctly_with_unix_line_endings()
        {
            RunDocumentChangeTest("\n");
        }

        private void RunDocumentChangeTest(string lineEnding)
        {
            string oldText = string.Join(lineEnding, "abc", "def", "", "abc", "abcdef");
            int oldTextLineCount = 5;
            int oldTextLastCharacterNum = 6;
            string newText = string.Join(lineEnding, "abc", "def");
            var testUri = new Uri("untitled://test");

            var docChange = MsSqlServiceClient.GetDocumentChangeForText(testUri, newText, oldText);

            docChange.ContentChanges.Length
                .Should()
                .Equals(1);
            docChange.ContentChanges[0].Range.End.Line
                .Should()
                .Equals(oldTextLineCount - 1);
            docChange.ContentChanges[0].Range.End.Character
                .Should()
                .Equals(oldTextLastCharacterNum);
            docChange.ContentChanges[0].Range.Start.Line
                .Should()
                .Equals(0);
            docChange.ContentChanges[0].Range.Start.Character
                .Should()
                .Equals(0);
            docChange.ContentChanges[0].Text
                .Should()
                .Equals(newText);
            docChange.TextDocument.Uri
                .Should()
                .Equals(testUri.ToString());
            docChange.TextDocument.Version
                .Should()
                .Equals(1);
        }
    }
}