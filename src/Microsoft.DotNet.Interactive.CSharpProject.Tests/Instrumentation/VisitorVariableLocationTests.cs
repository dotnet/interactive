// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;
using Microsoft.DotNet.Interactive.CSharpProject.Utility;
using Xunit;
using TextSpans = System.Collections.Generic.IDictionary<string, System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Text.TextSpan>>;
using LinePositionSpans = System.Collections.Generic.IDictionary<string, System.Collections.Immutable.ImmutableArray<Microsoft.CodeAnalysis.Text.LinePositionSpan>>;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests.Instrumentation;

using LinePosition = CodeAnalysis.Text.LinePosition;
using LinePositionSpan = CodeAnalysis.Text.LinePositionSpan;

public class VisitorVariableLocationTests
{

    private LinePositionSpans ConvertSpans(TextSpans spans, SourceText text)
    {
        var convertedKeyValues = spans.Select(kv =>
        {
            var convertedSpans = kv.Value.Select(span =>
            {
                var line = text.Lines.GetLineFromPosition(span.Start);
                var col = span.Start - line.Start;

                var endLine = text.Lines.GetLineFromPosition(span.End);
                var endcol = span.End - endLine.Start;

                return new LinePositionSpan(
                    new LinePosition(line.LineNumber, col),
                    new LinePosition(endLine.LineNumber, endcol)
                );
            });
            return new KeyValuePair<string, ImmutableArray<LinePositionSpan>>(kv.Key, convertedSpans.ToImmutableArray());
        });
        return convertedKeyValues.ToDictionary(x => x.Key, x => x.Value);
    }

    private async Task FindAndValidateVariablesAsync(string markup)
    {
        MarkupTestFile.GetNamedSpans(
            markup,
            out var text,
            out IDictionary<string, ImmutableArray<TextSpan>> spans);

        var document = Sources.GetDocument(text);
        var fileLineLocationSpans = ConvertSpans(spans, await document.GetTextAsync());

        var visitor = new InstrumentationSyntaxVisitor(document, await document.GetSemanticModelAsync());
        var locations = visitor.VariableLocations.Data.ToDictionary(
            key => key.Key.Name,
            values => values.Value.Select(location => location.ToLinePositionSpan()));

        foreach (var kv in locations)
        {
            var expected = new HashSet<LinePositionSpan>(fileLineLocationSpans[kv.Key]);
            var actual = new HashSet<LinePositionSpan>(kv.Value);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public async Task Pattern_Matching_Variables_Should_Be_Recorded()
    {
        await FindAndValidateVariablesAsync(@"
using System;
namespace RoslynRecorder
{
    class Program
    {
        static void Main({|args:string[] args|})
        {
            Console.WriteLine(""Entry Point"");
            object {|o:o|} = new object();
            if ({|o:o|} is string {|z:z|}){
                {|z:z|} = """";
                Console.WriteLine(""test"");
            }
        }
    }
}");
    }

    [Fact]
    public async Task Dynamic_Variable_Should_Be_Recorded()
    {
        await FindAndValidateVariablesAsync(@"

using System;
namespace RoslynRecorder
{
    class Program
    {
        static void Main({|args:string[] args|})
        {
            dynamic {|a:a|} = 0;
            Console.WriteLine(""Entry Point"");
        }
    }
}
");
    }
    [Fact]
    public async Task Property_Usages_Should_Be_Recorded()
    {
        await FindAndValidateVariablesAsync(@"
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main({|args:string[] args|})
        {
            Point {|p:p|} = new Point();
            {|p:p|}.x = 1;
            {|p:p|}.y = 2;
            Point {|p2:p2|} = {|p:p|};
            Console.WriteLine({|p2:p2|}.x);
        }

        class Point
        {
            public int x { get; set; }
            public int y { get; set; }
        }
    }
");
    }

    [Fact]
    public async Task Field_Usages_Should_Be_Recorded()
    {
        await FindAndValidateVariablesAsync(@"
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main({|args:string[] args|})
        {
            Point {|p:p|} = new Point();
            {|p:p|}.x = 1;
            {|p:p|}.y = 2;
            Point {|p2:p2|} = {|p:p|};
            Console.WriteLine({|p2:p2|}.x);
        }

        class Point
        {
            public int x;
            public int y;
        }
    }
");
    }

    [Fact]
    public async Task Out_Variable_Should_Be_Recorded()
    {
        await FindAndValidateVariablesAsync(@"
using System;

namespace ConsoleApp1
{
    class Program
    {
        static void Main({|args:string[] args|})
        {
            int {|i:i|} = 0;
            addOne({|i:i|}, out int {|j:j|});
            Console.WriteLine({|j:j|});

            void addOne({|a:int a|}, {|b:out int b|})
            {
                {|b:b|} = {|a:a|} + 1;
            }
        }

    }
}
");
    }

    [Fact]
    public async Task Variable_In_Simple_Lambda_Function_Should_Be_Recorded()
    {
        await FindAndValidateVariablesAsync(@"
using System;
using System.Linq;

namespace ConsoleApp1
{
    class Program
    {
        static void Main({|args:string[] args|})
        {
            var {|a:a|} = new[] { 1, 2 };
            var {|b:b|} = {|a:a|}.Select({|i:i|} => {|i:i|} + 1);
            Console.WriteLine({|b:b|});
        }

    }
}
");
    }

    [Fact]
    public async Task Variable_In_Parens_Lambda_Function_Should_Be_Recorded()
    {
        await FindAndValidateVariablesAsync(@"
using System;
using System.Linq;

namespace ConsoleApp1
{
    class Program
    {
        static void Main({|args:string[] args|})
        {
            var {|a:a|} = new[] { 1, 2 };
            var {|b:b|} = {|a:a|}.Select(({|i:i|}) => {
                return {|i:i|} + 1
            });
            Console.WriteLine({|b:b|});
        }

    }
}
");
    }

    [Fact]
    public async Task For_Loop_Variables_Should_Be_Recorded()
    {
        await FindAndValidateVariablesAsync(@"
using System;
using System.Linq;

namespace ConsoleApp1
{
    class Program
    {
        static void Main({|args:string[] args|})
        {
            for(int {|i:i|} = 0; {|i:i|} != 10; ++{|i:i|}){
                Console.WriteLine({|i:i|});
            }
        }

    }
}
");

    }

    [Fact]
    public async Task ForEach_Loop_Variables_Should_Be_Recorded()
    {
        await FindAndValidateVariablesAsync(@"
using System;
using System.Linq;

namespace ConsoleApp1
{
    class Program
    {
        static void Main({|args:string[] args|})
        {
            var {|a:a|} = new [] { 1, 2 };
            foreach(var {|i:i|} in {|a:a|}){
                Console.WriteLine({|i:i|});
            }
        }

    }
}
");


    }
}