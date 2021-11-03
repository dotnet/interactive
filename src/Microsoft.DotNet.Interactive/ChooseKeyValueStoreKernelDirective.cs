// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.CommandLine;
using System.CommandLine.Binding;
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

                    var fromUrlResult = result.FindResultFor(FromUrlOption);

                    if (fromUrlResult is { })
                    {
                        result.ErrorMessage =
                            $"The {fromUrlResult.Token.Value} and {((OptionResult)result.Parent).Token.Value} options cannot be used together.";
                        return null;
                    }
                    else if (!File.Exists(filePath))
                    {
                        result.ErrorMessage = Resources.Instance.FileDoesNotExist(filePath);
                        return null;
                    }
                    else
                    {
                        return new FileInfo(filePath);
                    }
                });

            MimeTypeOption = new Option<string>(
                    "--mime-type",
                    "A mime type for the value. If specified, displays the value immediately as an output using the specified mime type.")
                .AddSuggestions((_, __) => new[]
                {
                    "application/json",
                    "text/html",
                    "text/plain",
                    "text/csv"
                });

            Add(NameOption);
            Add(FromUrlOption);
            Add(FromFileOption);
            Add(MimeTypeOption);
        }

        protected override async Task Handle(KernelInvocationContext kernelInvocationContext,
            InvocationContext commandLineInvocationContext)
        {
            var options = ValueDirectiveOptions.Create(commandLineInvocationContext.ParseResult);
            var kernel = Kernel as KeyValueStoreKernel;


            {
                await kernel.TryStoreValueFromOptionsAsync(kernelInvocationContext, options);
            }

            await base.Handle(kernelInvocationContext, commandLineInvocationContext);
        }

        public Option<string> MimeTypeOption { get; }

        public Option<Uri> FromUrlOption { get; }

        public Option<FileInfo> FromFileOption { get; }

        public Option<string> NameOption { get; }

        internal class ValueDirectiveOptions
        {
            private static readonly ModelBinder<ValueDirectiveOptions> _modelBinder = new();

            public static ValueDirectiveOptions Create(ParseResult parseResult) =>
                _modelBinder.CreateInstance(new BindingContext(parseResult)) as ValueDirectiveOptions;

            public string Name { get; set; }

            public FileInfo FromFile { get; set; }

            public Uri FromUrl { get; set; }

            public string MimeType { get; set; }
        }
    }
}