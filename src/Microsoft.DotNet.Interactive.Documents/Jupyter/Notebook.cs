// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.DotNet.Interactive.Documents.Json;

namespace Microsoft.DotNet.Interactive.Documents.Jupyter;

public static class Notebook
{
    static Notebook()
    {
        JsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            NumberHandling = JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Converters =
            {
                new ByteArrayConverter(),
                new DataDictionaryConverter(),
                new InteractiveDocumentConverter(),
                new InteractiveDocumentElementConverter(),
                new InteractiveDocumentOutputElementConverter(),
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            }
        };
    }

    public static JsonSerializerOptions JsonSerializerOptions { get; }

    public const string MetadataNamespace = "dotnet_interactive";
    public const string PolyglotMetadataNamespace = "polyglot_notebook";

    public static Encoding Encoding => new UTF8Encoding(false);

    public static InteractiveDocument Parse(
        string json,
        KernelInfoCollection? kernelInfos = null)
    {
        var document = JsonSerializer.Deserialize<InteractiveDocument>(json, JsonSerializerOptions) ??
                       throw new JsonException($"Unable to parse as {typeof(InteractiveDocument)}:\n\n{json}");

        if (kernelInfos is not null)
        {
            document.NormalizeElementKernelNames(kernelInfos);
        }

        return document;
    }

    public static InteractiveDocument Read(
        Stream stream,
        KernelInfoCollection kernelInfos)
    {
        using var reader = new StreamReader(stream, Encoding);
        var content = reader.ReadToEnd();
        return Parse(content, kernelInfos);
    }

    public static void Write(InteractiveDocument document, Stream stream)
    {
        using var writer = new StreamWriter(stream, Encoding, 1024, true);
        Write(document, writer);
        writer.Flush();
    }

    public static void Write(InteractiveDocument document, Stream stream, KernelInfoCollection kernelInfos)
    {
        InteractiveDocument.MergeKernelInfos(document, kernelInfos);
        Write(document, stream);
    }

    public static string ToJupyterJson(
        this InteractiveDocument document,
        string? defaultLanguage = null)
    {
        if (defaultLanguage is { })
        {
            document.WithJupyterMetadata(defaultLanguage);
        }

        var json = JsonSerializer.Serialize(document, JsonSerializerOptions);

        var singleSpaceIndentedJson =
            Regex.Replace(
                json,
                "(^|\\G)( ){2}",
                " ", RegexOptions.Multiline);

        return singleSpaceIndentedJson;
    }

    public static void Write(InteractiveDocument document, TextWriter writer)
    {
        var content = document.ToJupyterJson();
        writer.Write(content);
    }
    public static void Write(InteractiveDocument document, TextWriter writer, KernelInfoCollection kernelInfos)
    {
        InteractiveDocument.MergeKernelInfos(document, kernelInfos);
        Write(document, writer);
    }

}