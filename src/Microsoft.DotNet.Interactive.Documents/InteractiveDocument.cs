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
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Documents.Json;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Documents.Utility;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Documents;

public class InteractiveDocument : IEnumerable
{
    static InteractiveDocument()
    {
        JsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new ByteArrayConverter(),
                new DataDictionaryConverter(),
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                new InteractiveDocumentConverter(),
            }
        };
    }

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

    public IList<InteractiveDocumentElement> Elements { get; }

    public IDictionary<string, object> Metadata =>
        _metadata ??= new Dictionary<string, object>();

    public async IAsyncEnumerable<InteractiveDocument> GetImportsAsync(bool recursive = false)
    {
        EnsureImportFieldParserIsInitialized();

        if (!TryGetKernelInfosFromMetadata(Metadata, out var kernelInfos))
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

    IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();

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

    public static async Task<InteractiveDocument> LoadAsync(
        FileInfo file,
        KernelInfoCollection? kernelInfos = null)
    {
        var fileContents = await IOExtensions.ReadAllTextAsync(file.FullName);

        return file.Extension.ToLowerInvariant() switch
        {
            // polyglot formats
            ".ipynb" => Notebook.Parse(fileContents, kernelInfos),
            ".dib" => CodeSubmission.Parse(fileContents, kernelInfos),

            // single-language formats
            ".cs" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "csharp") },
            ".csx" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "csharp") },
            ".fs" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "fsharp") },
            ".fsx" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "fsharp") },
            ".ps1" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "pwsh") },
            ".html" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "html") },
            ".http" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "http") },
            ".js" => new InteractiveDocument { new InteractiveDocumentElement(fileContents, "javascript") },

            _ => throw new InvalidOperationException($"Unrecognized extension for a notebook: {file.Extension}")
        };
    }

    public string? GetDefaultKernelName()
    {
        // FIX: (GetDefaultKernelName) remove from public API
        if (TryGetKernelInfosFromMetadata(Metadata, out var kernelInfo))
        {
            return kernelInfo.DefaultKernelName;
        }

        return null;
    }

    internal string? GetDefaultKernelName(KernelInfoCollection kernelInfos)
    {
        if (TryGetKernelInfosFromMetadata(Metadata, out var kernelInfoCollection))
        {
            return kernelInfoCollection.DefaultKernelName;
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

        return null;
    }

    internal static void MergeKernelInfos(InteractiveDocument document, KernelInfoCollection kernelInfos)
    {
        if (TryGetKernelInfosFromMetadata(document.Metadata, out var kernelInfoCollection))
        {
            MergeKernelInfos(kernelInfoCollection, kernelInfos);
        }
        else
        {
            document.Metadata["kernelInfo"] = kernelInfos;
        }
    }

    internal static void MergeKernelInfos(KernelInfoCollection destination, KernelInfoCollection source)
    {
        var added = new HashSet<string>();
        foreach (var kernelInfo in destination)
        {
            added.Add(kernelInfo.Name);
        }

        destination.AddRange(source.Where(ki => added.Add(ki.Name)));
    }

    internal static bool TryGetKernelInfosFromMetadata(
        IDictionary<string, object>? metadata,
        [NotNullWhen(true)] out KernelInfoCollection? kernelInfos)
    {
        if (metadata is not null)
        {
            if (metadata.TryGetValue("kernelInfo", out var kernelInfoObj))
            {
                if (kernelInfoObj is JsonElement kernelInfoJson &&
                    kernelInfoJson.Deserialize<KernelInfoCollection>(JsonSerializerOptions) is
                        { } kernelInfoDeserialized)
                {
                    kernelInfos = kernelInfoDeserialized;
                    return true;
                }

                // todo: the kernelInfo should not deserialize as a dictionary
                if (kernelInfoObj is Dictionary<string, object> kernelInfoAsDictionary)
                {
                    var deserializedKernelInfo = new KernelInfoCollection();
                    if (kernelInfoAsDictionary.TryGetValue("defaultKernelName", out var defaultKernelNameObj) &&
                       defaultKernelNameObj is string defaultKernelName)
                    {
                        deserializedKernelInfo.DefaultKernelName = defaultKernelName;
                    }

                    if (kernelInfoAsDictionary.TryGetValue("items",
                                                           out var items))
                    {
                        if (items is IEnumerable<object> itemList)
                        {
                            foreach (var item in itemList.Cast<IDictionary<string, object>>())
                            {
                                if (item.TryGetValue("name", out var nameObj) &&
                                    nameObj is string name)
                                {
                                    string? language = null;
                                    if (
                                    item.TryGetValue("language", out var languageObj) &&
                                        languageObj is string deserializedLanguage)
                                    {
                                        language = deserializedLanguage;
                                    }

                                    IReadOnlyCollection<string>? aliases = null;
                                    if (
                                        item.TryGetValue("aliases", out var aliasesObj) &&
                                        aliasesObj is object[] deserializedAliases)
                                    {
                                        aliases = deserializedAliases.Select(a => a.ToString()).ToArray();
                                    }

                                    deserializedKernelInfo.Add(new KernelInfo(name, language, aliases));
                                }
                            }
                            kernelInfos = deserializedKernelInfo;
                            return true;
                        }
                    }
                }

                if (kernelInfoObj is KernelInfoCollection kernelInfoCollection)
                {
                    kernelInfos = kernelInfoCollection;
                    return true;
                }
            }

            if (metadata.TryGetValue("dotnet_interactive", out var dotnetInteractiveObj))
            {
                switch (dotnetInteractiveObj)
                {
                    case KernelInfoCollection kernelInfoCollection:

                        kernelInfos = kernelInfoCollection;
                        return true;

                    case IDictionary<string, object> dotnetInteractiveDict:
                        {
                            kernelInfos = new();

                            if (dotnetInteractiveDict.TryGetValue("defaultKernelName", out var nameObj) &&
                                nameObj is string name)
                            {
                                kernelInfos.DefaultKernelName = name;
                            }

                            return true;
                        }
                }
            }

            // check for .ipynb / Jupyter metadata
            if (metadata.TryGetValue("kernelspec", out var kernelspecObj))
            {
                if (kernelspecObj is IDictionary<string, object> kernelspecDict)
                {
                    if (kernelspecDict.TryGetValue("language", out var languageObj) &&
                        languageObj is string defaultLanguage)
                    {
                        kernelInfos = new KernelInfoCollection
                        {
                            DefaultKernelName = defaultLanguage
                        };
                        return true;
                    }
                }
            }
        }

        // check if a KernelInfoCollection was directly serialized into the metadata
        kernelInfos = default;
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

    internal static JsonSerializerOptions JsonSerializerOptions { get; }
}