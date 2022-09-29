// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Documents.ParserServer;
using Microsoft.DotNet.Interactive.Documents.Utility;

namespace Microsoft.DotNet.Interactive.Documents;

public class InteractiveDocument : IEnumerable
{
    private static Parser? _importFieldsParser;
    private static Argument<FileInfo>? _importedFileArgument;

    private static Parser? _inputFieldsParser;
    private static Option<string>? _valueNameOption;
    private static Option<string[]>? _discoveredInputName;
    private static Option<string[]>? _discoveredPasswordName;

    private IDictionary<string, object>? _metadata;

    public InteractiveDocument(IList<InteractiveDocumentElement>? elements = null)
    {
        Elements = elements ?? new List<InteractiveDocumentElement>();
    }

    public IList<InteractiveDocumentElement> Elements { get; } = new List<InteractiveDocumentElement>();

    public IDictionary<string, object> Metadata =>
        _metadata ??= new Dictionary<string, object>();

    public async IAsyncEnumerable<InteractiveDocument> GetImportsAsync(bool recursive = false)
    {
        EnsureImportFieldParserIsInitialized();

        if (!TryGetKernelInfoFromMetadata(Metadata, out var kernelInfos))
        {
            kernelInfos = new();
        }

        foreach (var line in GetMagicCommandLines())
        {
            var parseResult = _importFieldsParser!.Parse(line);

            if (parseResult.CommandResult.Command.Name == "#!import")
            {
                if (!parseResult.Errors.Any())
                {
                    var file = parseResult.GetValueForArgument(_importedFileArgument!);

                    var interactiveDocument = await LoadAsync(file, kernelInfos);

                    yield return interactiveDocument;

                    if (recursive)
                    {
                        await foreach (var import in interactiveDocument.GetImportsAsync(recursive))
                        {
                            yield return import;
                        }
                    }
                }
                else if (parseResult.GetValueForArgument(_importedFileArgument!).FullName is { } file)
                {
                    throw new FileNotFoundException(file);
                }
            }
        }
    }

    public IEnumerable<InputField> GetInputFields()
    {
        EnsureInputFieldParserIsInitialized();

        var inputFields = new List<InputField>();

        foreach (var line in GetMagicCommandLines())
        {
            foreach (var field in ParseInputFields(line))
            {
                inputFields.Add(field);
            }
        }

        return inputFields.Distinct().ToArray();


        static IReadOnlyCollection<InputField> ParseInputFields(string line)
        {
            var inputFields = new List<InputField>();
            var result = _inputFieldsParser!.Parse(line);

            if (result.GetValueForOption(_discoveredInputName!) is { } inputNames)
            {
                foreach (var inputName in inputNames.Distinct())
                {
                    inputFields.Add(new InputField(inputName, "text"));
                }
            }

            if (result.GetValueForOption(_discoveredPasswordName!) is { } passwordNames)
            {
                foreach (var passwordName in passwordNames.Distinct())
                {
                    inputFields.Add(new InputField(passwordName, "password"));
                }
            }

            return inputFields;
        }
    }
    
    public IEnumerator GetEnumerator() => Elements.GetEnumerator();

    public void Add(InteractiveDocumentElement element) => Elements.Add(element);

    internal void NormalizeElementKernelNames(KernelInfoCollection kernelInfos)
    {
        var defaultKernelName = GetDefaultKernelName(kernelInfos);

        foreach (var element in Elements)
        {
            if (element.InferredTargetKernelName is not null &&
                kernelInfos.TryGetByAlias(element.InferredTargetKernelName, out var byMagic))
            {
                element.KernelName = byMagic.Name;
            }

            if (element.KernelName is null)
            {
                element.KernelName = defaultKernelName;
            }

            if (element.KernelName is not null &&
                kernelInfos.TryGetByAlias(element.KernelName, out var n))
            {
                element.KernelName = n.Name;
            }
        }
    }

    public string? GetDefaultKernelName()
    {
        if (TryGetKernelInfoFromMetadata(Metadata, out var kernelInfo))
        {
            return kernelInfo.DefaultKernelName;
        }

        return null;
    }

    public static async Task<InteractiveDocument> LoadAsync(
        FileInfo file,
        KernelInfoCollection kernelInfos)
    {
        var fileContents = await File.ReadAllTextAsync(file.FullName);

        return file.Extension.ToLowerInvariant() switch
        {
            ".ipynb" => Notebook.Parse(fileContents, kernelInfos),
            ".dib" => CodeSubmission.Parse(fileContents, kernelInfos),



            _ => throw new InvalidOperationException($"Unrecognized extension for a notebook: {file.Extension}"),
        };
    }

    internal string? GetDefaultKernelName(KernelInfoCollection kernelInfos)
    {
        string? defaultKernelName = null;

        if (Metadata is null)
        {
            return null;
        }

        if (Metadata.TryGetValue("kernelspec", out var kernelspecObj))
        {
            if (kernelspecObj is IDictionary<string, object> kernelspecDict)
            {
                if (kernelspecDict.TryGetValue("language", out var languageObj) &&
                    languageObj is string defaultLanguage)
                {
                    return defaultLanguage;
                }
            }
        }

        if (kernelInfos.DefaultKernelName is { } defaultFromKernelInfos)
        {
            if (kernelInfos.TryGetByAlias(defaultFromKernelInfos, out var info))
            {
                return info.Name;
            }
        }

        return defaultKernelName;
    }

    internal static bool TryGetKernelInfoFromMetadata(
        IDictionary<string, object>? metadata,
        [NotNullWhen(true)] out KernelInfoCollection? kernelInfo)
    {
        if (metadata?.TryGetValue("kernelInfo", out var kernelInfoObj) == true &&
            kernelInfoObj is JsonElement kernelInfoJson && kernelInfoJson.Deserialize<KernelInfoCollection>(ParserServerSerializer.JsonSerializerOptions) is
                { } kernelInfoDeserialized)
        {
            kernelInfo = kernelInfoDeserialized;
            return true;
        }

        kernelInfo = null;
        return false;
    }

    public IEnumerable<string> GetMagicCommandLines() =>
        Elements.SelectMany(e => e.Contents.SplitIntoLines())
                .Where(line => line.StartsWith("#!"));

    private static void EnsureImportFieldParserIsInitialized()
    {
        if (_importFieldsParser is not null)
        {
            return;
        }

        _importedFileArgument = new Argument<FileInfo>("file")
            .ExistingOnly();

        var importCommand = new Command("#!import")
        {
            _importedFileArgument
        };

        var rootCommand = new RootCommand
        {
            importCommand
        };

        _importFieldsParser = new CommandLineBuilder(rootCommand).Build();
    }

    private static void EnsureInputFieldParserIsInitialized()
    {
        if (_inputFieldsParser is not null)
        {
            return;
        }

        _valueNameOption = new Option<string>("--name");

        var valueCommand = new Command("#!value")
        {
            _valueNameOption
        };

        var rootCommand = new RootCommand
        {
            valueCommand
        };

        _discoveredInputName = new Option<string[]>("--discovered-input-name");
        _discoveredPasswordName = new Option<string[]>("--discovered-password-name");
        rootCommand.AddGlobalOption(_discoveredInputName);
        rootCommand.AddGlobalOption(_discoveredPasswordName);

        _inputFieldsParser = new CommandLineBuilder(rootCommand)
                             .UseTokenReplacer((string replace, out IReadOnlyList<string>? tokens, out string? message) =>
                             {
                                 if (replace.StartsWith("input:"))
                                 {
                                     tokens = new[] { _discoveredInputName.Aliases.First(), replace.Split(':')[1] };
                                     message = null;
                                     return true;
                                 }
                                 else if (replace.StartsWith("password:"))
                                 {
                                     tokens = new[] { _discoveredPasswordName.Aliases.First(), replace.Split(':')[1] };
                                     message = null;
                                     return true;
                                 }
                                 else
                                 {
                                     tokens = null;
                                     message = null;
                                     return false;
                                 }
                             })
                             .Build();
    }

}