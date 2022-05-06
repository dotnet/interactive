// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive
{
    public class ChooseKeyValueStoreKernelDirective : ChooseKernelDirective
    {
        public ChooseKeyValueStoreKernelDirective(Kernel kernel) : base(kernel,
            "Stores a value that can then be shared with other subkernels.")
        {
            NameOption = new Option<string>(
                "--name",
                "The name of the value to create. You can use #!share to retrieve this value from another subkernel.")
            {
                IsRequired = true
            };

            FromUrlOption = new Option<Uri>(
                "--from-url",
                description: "Specifies a URL whose content will be stored.");

            FromFileOption = new Option<FileInfo>(
                "--from-file",
                description: "Specifies a file whose contents will be stored.",
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
                        result.ErrorMessage = LocalizationResources.Instance.FileDoesNotExist(filePath);
                        return null;
                    }
                    else
                    {
                        return new FileInfo(filePath);
                    }
                });

            FromValueOption = new Option<string>(
                "--from-value",
                description: "Specifies a value to be stored directly. Specifying @input:value allows you to prompt the user for this value.",
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
                    "A mime type for the value. If specified, displays the value immediately as an output using the specified mime type.")
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
                    result.ErrorMessage =
                        $"The {otherOptionResult.Token.Value} and {((OptionResult)result.Parent).Token.Value} options cannot be used together.";

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
}