// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Tests;

[TestClass]
#pragma warning disable 8509
public class LanguageKernelAssemblyReferenceTests : LanguageKernelTestBase
{
    public LanguageKernelAssemblyReferenceTests(TestContext output) : base(output)
    {
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task it_can_load_assembly_references_using_r_directive_single_submission(Language language)
    {
        var kernel = CreateKernel(language);

        // F# strings treat \ as an escape character.  So do C# strings, except #r in C# is special, and doesn't.  F# usually uses @ strings for paths @"c:\temp\...."
        var dllPath = CreateDllInCurrentDirectory();

        var source = language switch
        {
            Language.FSharp => $@"#r @""{dllPath.FullName}""
typeof<Hello>.Name",

            Language.CSharp => $@"#r ""{dllPath.FullName}""
typeof(Hello).Name"
        };

        await SubmitCode(kernel, source);

        KernelEvents.Should().NotContainErrors();

        KernelEvents
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .Value
            .Should()
            .Be("Hello");
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task it_can_load_assembly_references_using_r_directive_separate_submissions(Language language)
    {
        var kernel = CreateKernel(language);

        // F# strings treat \ as an escape character.  So do C# strings, except #r in C# is special, and doesn't.  F# usually uses @ strings for paths @"c:\temp\...."
        var dllPath = CreateDllInCurrentDirectory();

        var source = language switch
        {
            Language.FSharp => new[]
            {
                $"#r @\"{dllPath.FullName}\"",
                "typeof<Hello>.Name"
            },

            Language.CSharp => new[]
            {
                $"#r \"{dllPath.FullName}\"",
                "typeof(Hello).Name"
            }
        };

        await SubmitCode(kernel, source);

        KernelEvents.Should().NotContainErrors();

        KernelEvents
            .Should()
            .ContainSingle<ReturnValueProduced>()
            .Which
            .Value
            .Should()
            .Be("Hello");
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    [DataRow(Language.FSharp)]
    public async Task it_can_load_assembly_references_using_r_directive_with_relative_path(Language language)
    {
        var kernel = CreateKernel(language);

        var dllName = CreateDllInCurrentDirectory();

        var code = language switch
        {
            Language.CSharp => $"#r \"{dllName.Name}\"",
            Language.FSharp => $"#r \"{dllName.Name}\""
        };

        var command = new SubmitCode(code);

        await kernel.SendAsync(command);

        KernelEvents.Should().NotContainErrors();

        KernelEvents.Should()
            .ContainSingle<CommandSucceeded>(c => c.Command == command);
    }

    [TestMethod]
    [DataRow(Language.CSharp)]
    // [DataRow(Language.FSharp)] Not supported in F#
    public async Task it_can_load_assembly_references_using_r_directive_with_relative_path_after_user_code_changes_current_directory(Language language)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        DisposeAfterTest(() => Directory.SetCurrentDirectory(currentDirectory));

        var kernel = CreateKernel(language);

        //Even when a user changes the current directory, loading from a relative path is not affected.
        await kernel.SendAsync(new SubmitCode("System.IO.Directory.SetCurrentDirectory(\"..\")"));

        var dllName = CreateDllInCurrentDirectory();

        var code = language switch
        {
            Language.CSharp => $"#r \"{dllName.Name}\"\nnew Hello()",
            Language.FSharp => $"#r \"{dllName.Name}\"\nnew Hello()"
        };

        var command = new SubmitCode(code);

        await kernel.SendAsync(command);

        KernelEvents.Should().NotContainErrors();

        KernelEvents.Should()
            .ContainSingle<CommandSucceeded>(c => c.Command == command);
    }

    private FileInfo CreateDllInCurrentDirectory()
    {
        var assemblyName = Guid.NewGuid().ToString("N");
        var dllName = assemblyName + ".dll";

        var systemRefLocation = typeof(object).GetTypeInfo().Assembly.Location;
        var systemReference = MetadataReference.CreateFromFile(systemRefLocation);

        CSharpCompilation.Create(assemblyName)
            .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(systemReference)
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText("public class Hello { }"))
            .Emit(dllName);

        return new FileInfo(Path.Combine(Directory.GetCurrentDirectory(), dllName));
    }
}