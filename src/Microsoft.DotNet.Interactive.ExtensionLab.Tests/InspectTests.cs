using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;

using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public sealed class InspectTests
    {
        [Fact]
        public async Task inspect_with_default_settings_produces_error_and_disagnostics_on_invalid_source_code()
        {
            using var kernel = new CompositeKernel() {
                new CSharpKernel()
            };

            await new InspectExtension().OnLoadAsync(kernel);

            var submission = @"
#!inspect

public class A
{
    public string P1 { get; set; }
";

            var result = await kernel.SendAsync(new SubmitCode(submission, "csharp"));


            result.KernelEvents
                  .ToSubscribedList()
                  .Should()
                  .ContainSingle<ErrorProduced>()
                  .Which
                  .FormattedValues
                  .Should()
                  .ContainSingle()
                  .Which
                  .Value
                  .Should()
                  .ContainAll("(3,35): error CS1513: } expected");
        }

        [Fact]
        public async Task inspect_with_default_settings_produces_calls_inspector_and_prints_output()
        {
            using var kernel = new CompositeKernel() {
                new CSharpKernel()
            };

            await new InspectExtension().OnLoadAsync(kernel);

            var submission = @"
#!inspect

public class A
{
    public string P1 { get; set; }
}
";

            var result = await kernel.SendAsync(new SubmitCode(submission, "csharp"));

            result.KernelEvents
                .ToSubscribedList()
                .Should()
                .NotContainErrors();

            var formattedValues = result.KernelEvents
                .ToSubscribedList()
                .Should()
                .ContainSingle<DisplayedValueProduced>()
                .Which
                .FormattedValues
                .Should()
                .ContainSingle()
                .Which
                .Value
                .Should()
                .ContainAll(
                    "Tabbed view ",
                    "[assembly: CompilationRelaxations(8)]", "[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]",
                    "private auto ansi ", "instance string get_P1 () cil managed",
                    "Script+A..ctor()", "Script+A.get_P1()");
        }
    }
}
