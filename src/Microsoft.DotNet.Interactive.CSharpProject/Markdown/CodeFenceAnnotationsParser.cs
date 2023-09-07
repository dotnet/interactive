// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Linq;
using Markdig;

namespace Microsoft.DotNet.Interactive.CSharpProject.Markdown;

public class CodeFenceAnnotationsParser
{
    private readonly IDefaultCodeBlockAnnotations _defaultAnnotations;
    private readonly Parser _parser;
    private readonly HashSet<string> _supportedLanguages;
    private const string PackageOptionName = "--package";
    private const string PackageVersionOptionName = "--package-version";
    private readonly Dictionary<Command, Lazy<ModelBinder>> _modelBindersByCommand;

    public CodeFenceAnnotationsParser(
        IDefaultCodeBlockAnnotations defaultAnnotations = null,
        Action<Command> configureCsharpCommand = null,
        Action<Command> configureFsharpCommand = null,
        Action<Command> configureConsoleCommand = null)
    {
        _defaultAnnotations = defaultAnnotations;

        var languageBinder =
            new Lazy<ModelBinder>(() => new ModelBinder(CodeBlockAnnotationsType));

        _modelBindersByCommand = new Dictionary<Command, Lazy<ModelBinder>>
        {
            [CreateCsharpCommand(configureCsharpCommand)] = languageBinder,
            [CreateFsharpCommand(configureFsharpCommand)] = languageBinder,
            [CreateConsoleCommand(configureConsoleCommand)] = new Lazy<ModelBinder>(() => new ModelBinder(typeof(OutputBlockAnnotations)))
        };

        _supportedLanguages = new HashSet<string>(_modelBindersByCommand.Keys.SelectMany(c => c.Aliases));

        var rootCommand = new RootCommand();

        foreach (var command in _modelBindersByCommand.Keys)
        {
            rootCommand.Add(command);
        }

        _parser = new Parser(rootCommand);
    }

    public virtual Type CodeBlockAnnotationsType => typeof(CodeBlockAnnotations);

    public virtual CodeFenceOptionsParseResult TryParseCodeFenceOptions(
        string line,
        MarkdownParserContext parserContext = null)
    {
        if (parserContext.TryGetDefaultCodeBlockAnnotations(out var defaults))
        {
            if (defaults.Package != null &&
                !line.Contains(PackageOptionName))
            {
                line += $" {PackageOptionName} {defaults.Package}";
            }

            if (defaults.PackageVersion != null &&
                !line.Contains(PackageVersionOptionName))
            {
                line += $" {PackageVersionOptionName} {defaults.PackageVersion}";
            }
        }

        var result = _parser.Parse(line);

        if (!_supportedLanguages.Contains(result.CommandResult.Command.Name) ||
            result.Tokens.Count == 1)
        {
            return CodeFenceOptionsParseResult.None;
        }

        if (result.Errors.Any())
        {
            return CodeFenceOptionsParseResult.Failed(new List<string>(result.Errors.Select(e => e.Message)));
        }

        var modelBinder = _modelBindersByCommand[result.CommandResult.Command].Value;

        var invocationContext = new InvocationContext(result);
        var bindingContext = invocationContext.BindingContext;
        var annotations = modelBinder.CreateInstance(bindingContext);
        // var annotations = modelBinder.CreateInstance(new BindingContext(result));

        switch (annotations)
        {
            case CodeBlockAnnotations codeBlockAnnotations:
                return CodeFenceOptionsParseResult.Succeeded(codeBlockAnnotations);

            case OutputBlockAnnotations outputBlockAnnotations:
                return CodeFenceOptionsParseResult.Succeeded(outputBlockAnnotations);

            case null:
                return CodeFenceOptionsParseResult.Failed($"Failed to bind annotations: {result}");

            default:
                return CodeFenceOptionsParseResult.Failed($"Unrecognized annotations type: {annotations}");
        }
    }

    private IEnumerable<Option> CreateCommandOptions()
    {
        yield return new Option<RelativeFilePath>("--destination-file", result =>
        {
            if (RelativeFilePath.TryParse(result.Tokens[0].Value, out var relativeFilePath))
            {
                return relativeFilePath;
            }

            return null;
        });

        yield return new Option<bool>("--editable", getDefaultValue: () => true);

        yield return new Option<bool>("--hidden", getDefaultValue: () => false);

        yield return new Option<string>("--region");

        var packageOption = new Option<string>(PackageOptionName);

        if (_defaultAnnotations?.Package is { } defaultPackage)
        {
            packageOption.SetDefaultValue(defaultPackage);
        }

        yield return packageOption;

        var packageVersionOption = new Option<string>(PackageVersionOptionName);

        if (_defaultAnnotations?.PackageVersion is { } defaultPackageVersion)
        {
            packageVersionOption.SetDefaultValue(defaultPackageVersion);
        }

        yield return packageVersionOption;

        yield return new Option<string>("--session");
    }

    private Command CreateCsharpCommand(Action<Command> configureCsharpCommand)
    {
        var csharp = new Command("csharp");

        foreach (var commandOption in CreateCommandOptions())
        {
            csharp.AddOption(commandOption);
        }

        configureCsharpCommand?.Invoke(csharp);

        csharp.AddAlias("CS");
        csharp.AddAlias("C#");
        csharp.AddAlias("CSHARP");
        csharp.AddAlias("cs");
        csharp.AddAlias("c#");



        return csharp;
    }

    private Command CreateFsharpCommand(Action<Command> configureFsharpCommand)
    {
        var fsharp = new Command("fsharp");

        foreach (var commandOption in CreateCommandOptions())
        {
            fsharp.AddOption(commandOption);
        }

        configureFsharpCommand?.Invoke(fsharp);

        fsharp.AddAlias("FS");
        fsharp.AddAlias("F#");
        fsharp.AddAlias("FSHARP");
        fsharp.AddAlias("fs");
        fsharp.AddAlias("f#");
        return fsharp;
    }

    private Command CreateConsoleCommand(Action<Command> configureConsoleCommand)
    {
        var console = new Command("console")
        {
            new Option<string>("--session")
        };

        configureConsoleCommand?.Invoke(console);

        return console;
    }
}