// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.DotNet.Interactive.App.ParserServer;

namespace Microsoft.DotNet.Interactive.Documents.Tests;

internal static class NotebookParserServerTestExtensions
{
    public static string AsUtf8String(this byte[] data)
    {
        return Encoding.UTF8.GetString(data);
    }

    public static NotebookParseOrSerializeRequest CreateSerializeRequestFromJson(
        string kernelInfoJson = "",
        string defaultLanguage = "csharp")
    {
        return NotebookParseOrSerializeRequest.FromJson(
            $$"""
              {
                "type": "serialize",
                "id": "2",
                "serializationType": "dib",
                "defaultLanguage": "{{defaultLanguage}}",
                "newLine": "\r\n",
                "document": {
                  "elements": [      
                    {
                      "executionOrder": 0,
                      "kernelName": "csharp",
                      "contents": "#r \"nuget: DotLanguage.InteractiveExtension, *-*\"",
                      "outputs": [
                      ]
                    },
                    {
                      "executionOrder": 0,
                      "kernelName": "dot",
                      "contents": "digraph Blah {\r\n    rankdir=\"LR\"\r\n    node [shape=\"box\"];\r\n    A -> B -> C;\r\n    B -> D;\r\n  }",
                      "outputs": []
                    }
                  ],
                  "metadata": {
                    {{kernelInfoJson}}
                  }
                }
              }

              """);
    }
}