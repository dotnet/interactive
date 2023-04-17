// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive;

public class ChooseKeyValueStoreKernelDirective : ChooseKernelDirective
{
    public ChooseKeyValueStoreKernelDirective(Kernel kernel) : base(kernel,
        "Stores a value that can then be shared with other subkernels.")
    {
        NameOption = new Option<string>(
            "--name",
            LocalizationResources.Magics_value_name_Description())
        {
            IsRequired = true
        };

        FromUrlOption = new Option<Uri>(
            "--from-url",
            description: LocalizationResources.Magics_value_from_url_Description());

        FromFileOption = new Option<FileInfo>(
            "--from-file",
            description: LocalizationResources.Magics_value_from_file_Description(),
            parseArgument: result =>
            {
                var filePath = result.Tokens.Single().Value;

                if (SetErrorIfAlsoUsed(FromUrlOption, result))
                {
                    return null;
                }
                    
                if (SetErrorIfAlsoUsed(FromValueOption, result))
                {
                    return null;
                }

                if (!File.Exists(filePath))
                {
                    result.ErrorMessage = LocalizationResources.FileDoesNotExist(filePath);
                    return null;
                }
                else
                {
                    return new FileInfo(filePath);
                }
            });

        FromValueOption = new Option<string>(
            "--from-value",
            description: LocalizationResources.Magics_value_from_value_Description(),
            parseArgument: result =>
            {
                if (SetErrorIfAlsoUsed(FromUrlOption, result))
                {
                    return null;
                }

                return result.Tokens.Single().Value;
            });

        MimeTypeOption = new Option<string>(
                "--mime-type",
                LocalizationResources.Magics_value_mime_type_Description())
            .AddCompletions(new[]
            {
                "application/json",
                "text/html",
                "text/plain",
                "text/csv"
            });

        Add(FromFileOption);
        Add(FromUrlOption);
        Add(FromValueOption);
        Add(MimeTypeOption);
        Add(NameOption);

        bool SetErrorIfAlsoUsed(Option otherOption, ArgumentResult result)
        {
            var otherOptionResult = result.FindResultFor(otherOption);

            if (otherOptionResult is { })
            {
                result.ErrorMessage = LocalizationResources.Magics_ErrorMessageCannotBeUsedTogether(otherOptionResult.Token.Value, ((OptionResult)result.Parent).Token.Value);

                return true;
            }

            return false;
        }
    }

    protected override async Task Handle(KernelInvocationContext kernelInvocationContext,
        InvocationContext commandLineInvocationContext)
    {
        var options = ValueDirectiveOptions.Create(commandLineInvocationContext.ParseResult, this);
        var kernel = Kernel as KeyValueStoreKernel;

        await kernel.TryStoreValueFromOptionsAsync(kernelInvocationContext, options);

        await base.Handle(kernelInvocationContext, commandLineInvocationContext);
    }

    public Option<string> MimeTypeOption { get; }

    public Option<Uri> FromUrlOption { get; }

    public Option<FileInfo> FromFileOption { get; }

    public Option<string> FromValueOption { get; }

    public Option<string> NameOption { get; }

    internal class ValueDirectiveOptions
    {
        public static ValueDirectiveOptions Create(ParseResult parseResult, ChooseKeyValueStoreKernelDirective directive) =>
            new()
            {
                Name = parseResult.GetValueForOption(directive.NameOption),
                FromFile = parseResult.GetValueForOption(directive.FromFileOption),
                FromUrl = parseResult.GetValueForOption(directive.FromUrlOption),
                FromValue = parseResult.GetValueForOption(directive.FromValueOption),
                MimeType = parseResult.GetValueForOption(directive.MimeTypeOption),
            };

        public string Name { get; init; }

        public FileInfo FromFile { get; init; }

        public Uri FromUrl { get; init; }

        public string FromValue { get; set; }

        public string MimeType { get; init; }
    }
}