// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown;

internal class CodeBlockAnnotations : CodeFenceAnnotations
{
    protected static int _sessionIndex;

    public CodeBlockAnnotations(
        RelativeFilePath destinationFile = null,
        string package = null,
        string region = null,
        string session = null,
        bool editable = false,
        bool hidden = false,
        string runArgs = null,
        ParseResult parseResult = null,
        string packageVersion = null) 
        : base(parseResult, session, runArgs)
    {
        DestinationFile = destinationFile;
        Package = package;
        Region = region;
        Session = session;
        PackageVersion = packageVersion;
        Editable = !hidden && editable;
        Hidden = hidden;

        if (string.IsNullOrWhiteSpace(Session) && Editable)
        {
            Session = $"Run{++_sessionIndex}";
        }
    }

    public virtual string Package { get; }
    public RelativeFilePath DestinationFile { get; }
    public string Region { get; }
    public string PackageVersion { get; }
    public bool Editable { get; }
    public bool Hidden { get; }

    public virtual Task<CodeBlockContentFetchResult> TryGetExternalContent() => 
        Task.FromResult(CodeBlockContentFetchResult.None);

    public virtual Task AddAttributes(AnnotatedCodeBlock block)
    {
        if (Package != null)
        {
            block.AddAttribute("data-trydotnet-package", Package);
        }
            
        if (PackageVersion != null)
        {
            block.AddAttribute("data-trydotnet-package-version", PackageVersion);
        }

        if (!string.IsNullOrWhiteSpace(NormalizedLanguage))
        {
            block.AddAttribute("data-trydotnet-language", NormalizedLanguage);
        }

        return Task.CompletedTask;
    }
}