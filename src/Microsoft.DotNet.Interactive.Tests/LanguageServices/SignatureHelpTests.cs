// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable 8509
namespace Microsoft.DotNet.Interactive.Tests.LanguageServices
{
    public class SignatureHelpTests : LanguageKernelTestBase
    {
        public SignatureHelpTests(ITestOutputHelper output) : base(output)
        {
        }

        private Task<KernelCommandResult> SendSignatureHelpRequest(Kernel kernel, string code, int line, int character)
        {
            var command = new RequestSignatureHelp(code, new LinePosition(line, character));
            return kernel.SendAsync(command);
        }

        [Theory]
        [InlineData(Language.CSharp, "int Add(int a, int b) => a + b;", "Add($$", 0, "int Add(int a, int b)", 0, "a")]
        [InlineData(Language.CSharp, "int Add(int a, int b) => a + b;", "Add($$)", 0, "int Add(int a, int b)", 0, "a")]
        [InlineData(Language.CSharp, "int Add(int a, int b) => a + b;", "Add(,$$", 0, "int Add(int a, int b)", 1, "b")]
        [InlineData(Language.CSharp, "int Add(int a, int b) => a + b;", "Add(1,$$", 0, "int Add(int a, int b)", 1, "b")]
        [InlineData(Language.CSharp, "int Add(int a, int b) => a + b;", "Add(1,$$)", 0, "int Add(int a, int b)", 1, "b")]
        [InlineData(Language.CSharp, "int Add(int a, int b) => a + b;\nint Sub(int c, int d) => c - d;", "Add(Sub($$", 0, "int Sub(int c, int d)", 0, "c")]
        [InlineData(Language.CSharp, "int Add(int a, int b) => a + b;\nint Sub(int c, int d) => c - d;", "Add(Sub(1, 2),$$", 0, "int Add(int a, int b)", 1, "b")]
        public async Task correct_signature_help_is_displayed(Language language, string submittedCode, string markupCode, int activeSignature, string signaureLabel, int activeParameter, string parameterName)
        {
            var kernel = CreateKernel(language);

            await kernel.SubmitCodeAsync(submittedCode);

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var column);

            await kernel.SendAsync(new RequestSignatureHelp(code, new LinePosition(line, column)));

            KernelEvents
                .Should()
                .ContainSingle<SignatureHelpProduced>()
                .Which
                .Signatures
                .Should()
                .HaveCountGreaterThan(activeSignature)
                .And
                .ContainSingle(signatureInfo => signatureInfo.Label == signaureLabel &&
                                                signatureInfo.Parameters.Count > activeParameter &&
                                                signatureInfo.Parameters[activeParameter].Label == parameterName);
        }

        [Fact]
        public async Task signature_help_can_handle_language_switching_and_offsets()
        {
            // switch to C# from an F# kernel/cell
            using var kernel = CreateCompositeKernel(Language.FSharp);
            var fullMarkupCode = string.Join("\r\n", new[]
            {
                "let x = 1",
                "#!csharp",
                "Console.WriteLine($$)"
            });

            MarkupTestFile.GetLineAndColumn(fullMarkupCode, out var code, out var line, out var character);
            var commandResult = await SendSignatureHelpRequest(kernel, code, line, character);

            commandResult
                .KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<SignatureHelpProduced>()
                .Which
                .Signatures
                .Should()
                .Contain(sigInfo => sigInfo.Label == "void Console.WriteLine()");
        }

        [Theory]
        [InlineData(Language.CSharp, @"
            /// <summary>
            /// Adds two numbers.
            /// </summary>
            int Add(int a, int b)
            {
                return a + b;
            }", "Add($$", 0, "Adds two numbers.\r\n")]
        public async Task signature_help_can_return_doc_comments_from_source(Language language, string submittedCode, string markupCode, int activeSignature, string expectedMethodDocumentation)
        {
            var kernel = CreateKernel(language);

            await kernel.SubmitCodeAsync(submittedCode);

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var column);

            await kernel.SendAsync(new RequestSignatureHelp(code, new LinePosition(line, column)));

            KernelEvents
                .Should()
                .ContainSingle<SignatureHelpProduced>()
                .Which
                .Signatures
                .Should()
                .HaveCountGreaterThan(activeSignature)
                .And
                .Subject
                .Should()
                .ContainSingle()
                .Which
                .Documentation.Value
                .Should()
                .Be(expectedMethodDocumentation);
        }

        [Fact]
        public async Task csharp_signature_help_contains_doc_comments_from_individually_referenced_assemblies_with_xml_files()
        {
            using var assembly = new TestAssemblyReference("Project", "netstandard2.0", "Program.cs", @"
public class SampleClass
{
    /// <summary>A sample class constructor.</summary>
    public SampleClass() { }
}
");
            var assemblyPath = await assembly.BuildAndGetPathToAssembly();

            using var kernel = CreateKernel(Language.CSharp);

            await SubmitCode(kernel, $"#r \"{assemblyPath}\"");

            var markupCode = "new SampleClass($$";

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var column);

            await kernel.SendAsync(new RequestSignatureHelp(code, new LinePosition(line, column)));

            KernelEvents
                .Should()
                .ContainSingle<SignatureHelpProduced>()
                .Which
                .Signatures
                .Should()
                .ContainSingle(sh => sh.Documentation.Value.Contains("A sample class constructor."));
        }

        [Fact]
        public async Task csharp_signature_hep_can_read_doc_comments_from_nuget_packages_after_forcing_the_assembly_to_load()
        {
            using var kernel = CreateKernel(Language.CSharp);

            await SubmitCode(kernel, "#r \"nuget: Newtonsoft.Json, 12.0.3\"");

            // The following line forces the assembly and the doc comments to be loaded
            await SubmitCode(kernel, "var _unused = Newtonsoft.Json.JsonConvert.Null;");

            var markupCode = "Newtonsoft.Json.JsonConvert.DeserializeObject($$";

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
            await kernel.SendAsync(new RequestSignatureHelp(code, new LinePosition(line, character)));

            KernelEvents
                .Should()
                .ContainSingle<SignatureHelpProduced>()
                .Which
                .Signatures
                .Should()
                .ContainSingle(sh => sh.Documentation.Value.Contains("Deserializes the JSON to a .NET object."));
        }

        [Fact(Skip = "https://github.com/dotnet/interactive/issues/1071  N.b., the preceeding test can be deleted when this one is fixed.")]
        public async Task csharp_signature_help_can_read_doc_comments_from_nuget_packages()
        {
            using var kernel = CreateKernel(Language.CSharp);

            await SubmitCode(kernel, "#r \"nuget: Newtonsoft.Json, 12.0.3\"");

            var markupCode = "Newtonsoft.Json.JsonConvert.DeserializeObject($$";

            MarkupTestFile.GetLineAndColumn(markupCode, out var code, out var line, out var character);
            await kernel.SendAsync(new RequestSignatureHelp(code, new LinePosition(line, character)));

            KernelEvents
                .Should()
                .ContainSingle<SignatureHelpProduced>()
                .Which
                .Signatures
                .Should()
                .ContainSingle(sh => sh.Documentation.Value.Contains("Deserializes the JSON to a .NET object."));
        }
    }
}
