// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.DotNet.Interactive.Journey
{
    public class NotebookLessonParser
    {
        private static readonly Dictionary<string, LessonDirective> _stringToLessonDirectiveMap = new()
        {
            { "Challenge", LessonDirective.Challenge }
        };

        private static readonly Dictionary<string, ChallengeDirective> _stringToChallengeDirectiveMap = new()
        {
            { "ChallengeSetup", ChallengeDirective.ChallengeSetup },
            { "Question", ChallengeDirective.Question },
            { "Scratchpad", ChallengeDirective.Scratchpad }
        };

        private static List<string>? _allDirectiveNames = null;

        public static List<string> AllDirectiveNames =>
            _allDirectiveNames ??= _stringToLessonDirectiveMap.Keys.Concat(_stringToChallengeDirectiveMap.Keys).ToList();

        public static async Task<InteractiveDocument> ReadFileAsInteractiveDocument(
            FileInfo file,
            CompositeKernel? kernel = null)
        {
            await using var stream = file.OpenRead();

            var kernelNames = GetKernelNames(kernel);

            var notebook = file.Extension.ToLowerInvariant() switch
            {
                ".ipynb" => await Notebook.ReadAsync(stream, kernelNames),
                ".dib" => await CodeSubmission.ReadAsync(stream, "csharp", kernelNames),
                _ => throw new InvalidOperationException($"Unrecognized extension for a notebook: {file.Extension}"),
            };

            return notebook;
        }

        public static async Task<InteractiveDocument> LoadNotebookFromUrl(
            Uri uri,
            HttpClient? httpClient = null,
            CompositeKernel? kernel = null)
        {
            var client = httpClient ?? new HttpClient();
            var response = await client.GetAsync(uri);
            var content = await response.Content.ReadAsStringAsync();

            // FIX: (LoadNotebookFromUrl) differentiate file formats

            return CodeSubmission.Parse(content, "csharp", GetKernelNames(kernel));
        }

        private static List<KernelName> GetKernelNames(CompositeKernel? kernel)
        {
            List<KernelName> kernelNames = new();

            if (kernel is { })
            {
                var kernelChoosers = kernel.Directives.OfType<ChooseKernelDirective>();

                foreach (var kernelChooser in kernelChoosers)
                {
                    var kernelAliases = kernelChooser.Aliases.Select(alias => alias[2..]).ToList();

                    kernelNames.Add(new KernelName(kernelChooser.Name[2..], kernelAliases));
                }

                if (kernelNames.All(n => n.Name != "markdown"))
                {
                    kernelNames.Add(new KernelName("markdown", new[] { "md" }));
                }
            }
            else
            {
                kernelNames = new List<KernelName>
                {
                    new("csharp", new[] { "cs", "C#", "c#" }),
                    new("fsharp", new[] { "fs", "F#", "f#" }),
                    new("pwsh", new[] { "powershell" }),
                    new("markdown", new[] { "md" }),
                };
            }

            return kernelNames;
        }

        public static void Parse(InteractiveDocument document, out LessonDefinition lesson, out List<ChallengeDefinition> challenges)
        {
            List<InteractiveDocumentElement> rawSetup = new();
            List<List<InteractiveDocumentElement>> rawChallenges = new();
            List<string> challengeNames = new();

            var indexOfFirstLessonDirective = 0;

            while (indexOfFirstLessonDirective < document.Elements.Count
                && !TryParseLessonDirectiveCell(document.Elements[indexOfFirstLessonDirective], out var _, out _, out _))
            {
                rawSetup.Add(document.Elements[indexOfFirstLessonDirective]);
                indexOfFirstLessonDirective++;
            }

            var setup = rawSetup.Select(c => new SubmitCode(c.Contents)).ToList();

            List<InteractiveDocumentElement> currentChallenge = new();
            var cellCount = document.Elements.Count;
            for (var i = indexOfFirstLessonDirective; i < cellCount;)
            {
                if (TryParseLessonDirectiveCell(document.Elements[i], out var remainingCell, out var _, out var challengeName) &&
                    remainingCell is { } &&
                    challengeName is { })
                {
                    if (!string.IsNullOrWhiteSpace(remainingCell.Contents))
                    {
                        currentChallenge.Add(remainingCell);
                    }
                    challengeNames.Add(challengeName);
                    i++;
                }
                else
                {
                    while (i < cellCount &&
                          !TryParseLessonDirectiveCell(document.Elements[i], out var _, out var _, out var _))
                    {
                        currentChallenge.Add(document.Elements[i]);
                        i++;
                    }
                    rawChallenges.Add(currentChallenge);
                    currentChallenge = new();
                }
            }
            rawChallenges.Add(currentChallenge);

            List<ChallengeDefinition> challengeDefinitions = new();
            HashSet<string> challengeNamesSet = new();
            var index = 1;
            foreach (var (name, challengeCells) in challengeNames.Zip(rawChallenges, (name, challengeCells) => (name, challengeCells)))
            {
                var challengeName = string.IsNullOrWhiteSpace(name) ? $"Challenge {index}" : name;
                if (!challengeNamesSet.Add(challengeName))
                {
                    throw new ArgumentException($"{challengeName} conflicts with an existing challenge name");
                }

                challengeDefinitions.Add(ParseChallenge(challengeCells, challengeName));

                index++;
            }

            if (challengeDefinitions.Count == 0)
            {
                throw new ArgumentException($"This lesson has no challenges");
            }

            challenges = challengeDefinitions;
            // todo: what is lesson name?
            lesson = new LessonDefinition("", setup);
        }

        private static ChallengeDefinition ParseChallenge(List<InteractiveDocumentElement> cells, string name)
        {
            List<InteractiveDocumentElement> rawSetup = new();
            List<InteractiveDocumentElement> rawEnvironmentSetup = new();
            List<InteractiveDocumentElement> rawContents = new();

            int indexOfFirstChallengeDirective = 0;

            while (indexOfFirstChallengeDirective < cells.Count
                && !TryParseChallengeDirectiveElement(cells[indexOfFirstChallengeDirective], out var _, out _, out _))
            {
                rawSetup.Add(cells[indexOfFirstChallengeDirective]);
                indexOfFirstChallengeDirective++;
            }

            string? currentDirective = null;
            for (var i = indexOfFirstChallengeDirective; i < cells.Count;)
            {
                if (TryParseChallengeDirectiveElement(cells[i], out var remainingCell, out var directive, out _))
                {
                    currentDirective = directive;
                    if (currentDirective is { } &&
                        !string.IsNullOrWhiteSpace(remainingCell?.Contents))
                    {
                        AddChallengeComponent(currentDirective, remainingCell);
                    }
                    i++;
                }
                else
                {
                    while (i < cells.Count &&
                        !TryParseChallengeDirectiveElement(cells[i], out var _, out var _, out var _) &&
                        currentDirective is { })
                    {
                        AddChallengeComponent(currentDirective, cells[i]);
                        i++;
                    }
                }
            }

            if (rawContents.Count == 0)
            {
                throw new ArgumentException($"Challenge {name} has an empty question");
            }

            var setup = rawSetup.Select(c => new SubmitCode(c.Contents)).ToList();
            var contents = rawContents.Select(c => new SendEditableCode(c.Language, c.Contents)).ToList();
            var environmentSetup = rawEnvironmentSetup.Select(c => new SubmitCode(c.Contents)).ToList();

            return new ChallengeDefinition(name, setup, contents, environmentSetup);

            void AddChallengeComponent(string directiveName, InteractiveDocumentElement cell)
            {
                var directive = _stringToChallengeDirectiveMap[directiveName];
                switch (directive)
                {
                    case ChallengeDirective.ChallengeSetup:
                        rawEnvironmentSetup.Add(cell);
                        break;
                    case ChallengeDirective.Question:
                        rawContents.Add(cell);
                        break;
                    default:
                        break;
                }
            }
        }

        private static bool TryParseLessonDirectiveCell(InteractiveDocumentElement cell, out InteractiveDocumentElement? remainingCell, out string? directive, out string? afterDirective)
        {
            if (!TryParseDirectiveElement(cell, out directive, out afterDirective, out remainingCell))
            {
                return false;
            }
            return _stringToLessonDirectiveMap.Keys.Contains(directive);
        }

        private static bool TryParseChallengeDirectiveElement(InteractiveDocumentElement cell, out InteractiveDocumentElement? remainingCell, out string? directive, out string? afterDirective)
        {
            if (!TryParseDirectiveElement(cell, out directive, out afterDirective, out remainingCell))
            {
                return false;
            }
            return _stringToChallengeDirectiveMap.Keys.Contains(directive);
        }

        private static bool TryParseDirectiveElement(InteractiveDocumentElement cell, out string? directive, out string? afterDirective, out InteractiveDocumentElement? remainingCell)
        {
            directive = null;
            afterDirective = null;
            remainingCell = null;

            if (cell.Language != "markdown")
            {
                return false;
            }

            var result = Regex.Split(cell.Contents, "\r\n|\r|\n");

            var directivePattern = @"[^\[]*\[(?<directive>[a-zA-z]+)\][ ]?(?<afterDirective>[\S]*)";
            var match = Regex.Match(result[0], directivePattern);
            if (!match.Success)
            {
                return false;
            }

            directive = match.Groups["directive"].Value;
            afterDirective = match.Groups["afterDirective"].Value;
            remainingCell = new InteractiveDocumentElement(cell.Language, string.Join(Environment.NewLine, result.Skip(1)));
            return true;
        }
    }
}
