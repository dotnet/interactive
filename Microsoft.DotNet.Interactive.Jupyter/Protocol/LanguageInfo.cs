// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.DotNet.Interactive.Jupyter.Protocol
{
    public class LanguageInfo
    {
        [JsonProperty("name")]
        public string Name { get; }

        [JsonProperty("version")]
        public string Version { get; }

        [JsonProperty("mimetype")]
        public string MimeType { get;  }

        [JsonProperty("file_extension")]
        public string FileExtension { get; }

        [JsonProperty("pygments_lexer")]
        public string PygmentsLexer { get;  }

        [JsonProperty("codemirror_mode", NullValueHandling = NullValueHandling.Ignore)]
        public object CodeMirrorMode { get; set; }

        [JsonProperty("nbconvert_exporter", NullValueHandling = NullValueHandling.Ignore)]
        public string NbConvertExporter { get;  }

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
        public CSharpLanguageInfo(string version = "8.0") : base("C#", version, "text/x-csharp", ".cs", pygmentsLexer: "csharp")
        {
        }
    }

    public class FSharpLanguageInfo : LanguageInfo
    {
        public FSharpLanguageInfo(string version = "4.5") : base("C#", version, "text/x-fsharp", ".fs", pygmentsLexer: "fsharp")
        {
           
        }
    }

    public class PowerShellLanguageInfo : LanguageInfo
    {
        public PowerShellLanguageInfo(string version = "7.0") : base("PowerShell", version, "text/x-powershell", ".ps1", pygmentsLexer: "powershell")
        {
           
        }
    }

    public class VBnetLanguageInfo : LanguageInfo
    {
        public VBnetLanguageInfo(string version = "15.0") : base("C#", version, "text/x-vbnet", ".vb", pygmentsLexer: "vbnet")
        {
        }
    }
}