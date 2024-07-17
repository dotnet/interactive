// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;

using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests;

public sealed class InspectTests
{
    [Fact]
    public async Task inspect_with_default_settings_produces_error_and_diagnostics_on_invalid_source_code()
    {
        using var kernel = new CompositeKernel {
            new CSharpKernel()
        };

        await InspectExtension.LoadAsync(kernel);

        var submission = @"
#!inspect

public class A
{
    public string P1 { get; set; }
";

        var result = await kernel.SendAsync(new SubmitCode(submission, "csharp"));

        result.Events
            .Should()
            .ContainSingle<CommandFailed>()
            .Which
            .Message
            .Should()
            .ContainAll(",35): error CS1513: } expected");
    }

    [Fact]
    public async Task inspect_with_default_settings_calls_inspector_and_produces_output()
    {
        using var kernel = new CompositeKernel() {
            new CSharpKernel()
        };

        await InspectExtension.LoadAsync(kernel);

        var submission = @"
#!inspect

public class A
{
    public string P1 { get; set; }
}
";

        var result = await kernel.SendAsync(new SubmitCode(submission, "csharp"));

        result.Events
            .Should()
            .NotContainErrors();

        result.Events
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
                "ctor()", "get_P1()");
    }

    [Fact]
    public async Task inspect_with_complex_source_and_release_settings_calls_inspector_and_produces_output()
    {
        using var kernel = new CompositeKernel 
        {
            new CSharpKernel()
        };

        await InspectExtension.LoadAsync(kernel);

        var submission = """
                           #!inspect --configuration Release --kind Regular

                           using System;
                           public static class A {
                               public static int Sum(ReadOnlySpan<int> source)
                               {
                                   int result = 0;
                           
                                   for (int i = 0; i < source.Length; i++)
                                   {
                                       result += source[i];
                                   }
                           
                                   return result;
                               }
                           
                               static unsafe void Unsafe() {
                                       int var = 20;
                                       int* p = &var;
                               }
                           }

                           """;

        var result = await kernel.SendAsync(new SubmitCode(submission, "csharp"));

        result.Events
            .Should()
            .NotContainErrors();

        result.Events
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
                "private auto ansi ", "public static int Sum(ReadOnlySpan", "private static void Unsafe()");
    }

    [Theory]
    [InlineData("Release")]
    [InlineData("Debug")]
    public async Task inspect_with_configuration_settings_calls_inspector_and_produces_output(string configuration)
    {
        using var kernel = new CompositeKernel
        {
            new CSharpKernel()
        };

        await InspectExtension.LoadAsync(kernel);

        var submission = @$"
#!inspect --configuration {configuration}

public class A
{{
    public string P1 {{ get; set; }}
}}
";

        var result = await kernel.SendAsync(new SubmitCode(submission, "csharp"));

        result.Events
            .Should()
            .NotContainErrors();

        result.Events
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
                "[assembly: CompilationRelaxations(8)]", 
                "[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]",
                "private auto ansi ", 
                "instance string get_P1 () cil managed",
                "ctor()", 
                "get_P1()");
    }

    [Theory(Skip = "Utility will be determined later")]
    [InlineData("Script")]
    [InlineData("Regular")]
    public async Task inspect_with_kind_settings_calls_inspector_and_produces_output(string kind)
    {
        using var kernel = new CompositeKernel() {
            new CSharpKernel()
        };

        await InspectExtension.LoadAsync(kernel);

        var submission = @$"
#!inspect -k {kind}
using System;
public class A
{{
    public string P1 {{ get; set; }}
    public A(string p)
    {{
        this.P1 = p ?? throw new ArgumentNullException(nameof(p));
    }}
}}
";

        var result = await kernel.SendAsync(new SubmitCode(submission, "csharp"));

        result.Events
            .Should()
            .NotContainErrors();

        result.Events
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
                "ctor", "get_P1()");
    }

    [Fact]
    public async Task inspect_with_default_settings_produces_proper_js_and_css()
    {
        using var kernel = new CompositeKernel() {
            new CSharpKernel()
        };

        await InspectExtension.LoadAsync(kernel);

        var submission = @"
#!inspect

public class A
{
    public string P1 { get; set; }
}
";

        var result = await kernel.SendAsync(new SubmitCode(submission, "csharp"));

        result.Events
            .Should()
            .NotContainErrors();

        result.Events
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
                "cdn.jsdelivr.net/npm/prismjs@1.21.0/prism.min.js",
                "cdn.jsdelivr.net/npm/prismjs@1.21.0/themes/prism-coy.min.css");
    }
}