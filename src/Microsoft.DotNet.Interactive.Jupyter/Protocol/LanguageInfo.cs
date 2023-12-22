// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol;

public class LanguageInfo
{
    [JsonPropertyName("name")]
    public string Name { get; }

    [JsonPropertyName("version")]
    public string Version { get; }

    [JsonPropertyName("mimetype")]
    public string MimeType { get; }

    [JsonPropertyName("file_extension")]
    public string FileExtension { get; }

    [JsonPropertyName("pygments_lexer")]
    public string PygmentsLexer { get; }

    [JsonPropertyName("codemirror_mode")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object CodeMirrorMode { get; set; }

    [JsonPropertyName("nbconvert_exporter")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string NbConvertExporter { get; }

    public LanguageInfo(string name, string version, string mimeType, string fileExtension, string pygmentsLexer = null, object codeMirrorMode = null, string nbConvertExporter = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(version));
        }

        if (string.IsNullOrWhiteSpace(mimeType))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(mimeType));
        }

        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(fileExtension));
        }
        Name = name;
        Version = version;
        MimeType = mimeType;
        FileExtension = fileExtension;
        PygmentsLexer = pygmentsLexer;
        CodeMirrorMode = codeMirrorMode;
        NbConvertExporter = nbConvertExporter;
    }
}

public class CSharpLanguageInfo : LanguageInfo
{
    public CSharpLanguageInfo(string version = "11.0") : base("C#", version, "text/x-csharp", ".cs", pygmentsLexer: "csharp")
    {
    }
}

public class FSharpLanguageInfo : LanguageInfo
{
    public FSharpLanguageInfo(string version = "8.0") : base("F#", version, "text/x-fsharp", ".fs", pygmentsLexer: "fsharp")
    {
    }
}

public class PowerShellLanguageInfo : LanguageInfo
{
    public PowerShellLanguageInfo(string version = "7.0") : base("PowerShell", version, "text/x-powershell", ".ps1", pygmentsLexer: "powershell")
    {
    }
}
