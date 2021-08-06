// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.Interactive.dib;
using Microsoft.DotNet.Interactive.Ipynb;

namespace Microsoft.DotNet.Interactive
{
    public static class NotebookFileFormatHandler
    {
        public static InteractiveDocument Parse(string fileName, string content, string defaultLanguage,
            IDictionary<string, string> kernelLanguageAliases)
        {
            var extension = Path.GetExtension(fileName);
            switch (extension.ToLowerInvariant())
            {
                case ".dib":
                case ".dotnet-interactive":
                    return DibFile.Parse(content, defaultLanguage, kernelLanguageAliases);
                case ".ipynb":
                    return IpynbFile.Parse(content, kernelLanguageAliases);
                default:
                    throw new NotSupportedException($"Unable to parse a interactive document of type '{extension}'");
            }
        }

        public static InteractiveDocument Read(string fileName, Stream stream, string defaultLanguage, IDictionary<string, string> kernelLanguageAliases)
        {
            var extension = Path.GetExtension(fileName);
            switch (extension.ToLowerInvariant())
            {
                case ".dib":
                case ".dotnet-interactive":
                    return DibFile.Read(stream, defaultLanguage, kernelLanguageAliases);
                case ".ipynb":
                    return IpynbFile.Read(stream, kernelLanguageAliases);
                default:
                    throw new NotSupportedException($"Unable to parse a interactive document of type '{extension}'");
            }
        }


        public static void Write(string fileName, InteractiveDocument interactive, string newline, Stream stream)
        {
            var extension = Path.GetExtension(fileName);
          
            switch (extension.ToLowerInvariant())
            {
                case ".dib":
                case ".dotnet-interactive":
                    DibFile.Write(interactive, newline, stream);
                    break;
                case ".ipynb":
                    IpynbFile.Write(interactive, newline, stream);
                    break;
                default:
                    throw new NotSupportedException($"Unable to serialize a interactive document of type '{extension}'");
            }
        }
    }
}
