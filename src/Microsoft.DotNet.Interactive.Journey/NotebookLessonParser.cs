// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;

#nullable enable

namespace Microsoft.DotNet.Interactive.Journey;

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

    public static InteractiveDocument ReadFileAsInteractiveDocument(
        FileInfo file,
        CompositeKernel? kernel = null)
    {
        using var stream = file.OpenRead();

        var kernelInfo = GetKernelInfoFromKernel(kernel);

        var notebook = file.Extension.ToLowerInvariant() switch
        {
            ".ipynb" => Notebook.Read(stream, kernelInfo),
            ".dib" => CodeSubmission.Read(stream, kernelInfo),
            _ => throw new InvalidOperationException($"Unrecognized extension for a notebook: {file.Extension}")
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

        // TODO: (LoadNotebookFromUrl) differentiate file formats

        return CodeSubmission.Parse(content, GetKernelInfoFromKernel(kernel));
    }

    private static KernelInfoCollection GetKernelInfoFromKernel(CompositeKernel? kernel)
    {
        if (kernel is { })
        {
            KernelInfoCollection kernelInfos = new();

            foreach (var subkernel in kernel)
            {
                var info = subkernel.KernelInfo;
                kernelInfos.Add(new(info.LocalName, info.LanguageName, info.Aliases));
            }

            kernelInfos.DefaultKernelName = kernel.DefaultKernelName;

            if (kernelInfos.All(n => n.Name != "markdown"))
            {
                kernelInfos.Add(new Documents.KernelInfo("markdown", "Markdown", new[] { "md" }));
            }

            return kernelInfos;
        }
        else
        {
            var names = new KernelInfoCollection
            {
                new("csharp", "C#", new[] { "cs", "c#" }),
                new("fsharp", "F#", new[] { "fs", "f#" }),
                new("pwsh", "PowerShell", new[] { "powershell" }),
                new("markdown", "Markdown", new[] { "md" })
            };
            names.DefaultKernelName = "csharp";
            return names;
        }
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
        var contents = rawContents.Select(c => new SendEditableCode(c.KernelName, c.Contents)).ToList();
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

        if (cell.KernelName != "markdown")
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
        remainingCell = new InteractiveDocumentElement(string.Join(Environment.NewLine, result.Skip(1)), cell.KernelName);
        return true;
    }
}