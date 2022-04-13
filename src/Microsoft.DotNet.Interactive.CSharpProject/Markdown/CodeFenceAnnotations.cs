// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown
{
    public abstract class CodeFenceAnnotations
    {
        protected CodeFenceAnnotations(
            ParseResult parseResult,
            string session = null,
            string runArgs = null)
        {
            ParseResult = parseResult ?? throw new ArgumentNullException(nameof(parseResult));
            Session = session;

            NormalizedLanguage = parseResult?.CommandResult.Command.Name;
            Language = parseResult?.Tokens.First().Value;

            RunArgs = runArgs ?? Untokenize(parseResult);
        }

        public ParseResult ParseResult { get; }

        public string RunArgs { get; }

        public string Session { get; protected set; }

        public string Language { get; }

        public string NormalizedLanguage { get; }

        private static string Untokenize(ParseResult result) =>
            result == null
                ? null
                : string.Join(" ", result.Tokens
                                         .Select(t => t.Value)
                                         .Skip(1)
                                         .Select(t => Regex.IsMatch(t, @".*\s.*")
                                                          ? $"\"{t}\""
                                                          : t));
    }
}