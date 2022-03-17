// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.CSharpProject.Servers.Roslyn.Instrumentation;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests.Instrumentation
{
    public class CodeRewritingTests
    {
        [Fact]
        public async Task Rewritten_program_with_1_statements_has_1_calls_to_EmitProgramState()
        {
            var rewrittenCode = await RewriteCodeWithInstrumentation(@"
using System;

namespace ConsoleApp2
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine(""Hello World!"");
        }
    }
}");

            var emitterTypeName = typeof(InstrumentationEmitter).FullName;

            string expected = $@"
using System;

namespace ConsoleApp2
{{
    class Program
    {{
        static void Main()
        {{
{emitterTypeName}.EmitProgramState({emitterTypeName}.GetProgramState(""{{\""line\"":9,\""character\"":12,\""file\"":\""document.cs\""}}""));
            Console.WriteLine(""Hello World!"");
        }}
    }}
}}".EnforceLF();

            rewrittenCode.ShouldMatchLineByLine(expected);
        }

        [Fact]
        public async Task Rewritten_program_with_2_statements_has_2_calls_to_EmitProgramState()
        {
            string actual = await RewriteCodeWithInstrumentation(@"
using System;

namespace ConsoleApp2
{
    class Program
    {
        static void Main()
        {
            int a = 1;
            Console.WriteLine(""Hello World!"");
        }
    }
}");

            var emitterTypeName = typeof(InstrumentationEmitter).FullName;

            string expected = $@"
using System;

namespace ConsoleApp2
{{
    class Program
    {{
        static void Main()
        {{
                System.Console.WriteLine(""6a2f74a2-f01d-423d-a40f-726aa7358a81{{\""variableLocations\"": [{{    \""name\"": \""a\"",    \""locations\"": [{{    \""startLine\"": 9,    \""startColumn\"": 16,    \""endLine\"": 9,    \""endColumn\"": 17}}],    \""declaredAt\"": {{        \""start\"": 117,        \""end\"": 118    }}}}]}}6a2f74a2-f01d-423d-a40f-726aa7358a81"");
                {emitterTypeName}.EmitProgramState({emitterTypeName}.GetProgramState(""{{\""line\"":9,\""character\"":12,\""file\"":\""document.cs\""}}""));
                int a = 1;
                {emitterTypeName}.EmitProgramState({emitterTypeName}.GetProgramState(""{{\""line\"":10,\""character\"":12,\""file\"":\""document.cs\""}}"",(""{{\""name\"":\""a\"",\""value\"":\""unavailable\"",\""declaredAt\"":{{\""start\"":117,\""end\"":118}}}}"",a)));
                Console.WriteLine(""Hello World!"");
        }}
    }}
}}".EnforceLF();
            actual.ShouldMatchLineByLine(expected);
        }

        private async Task<string> RewriteCodeWithInstrumentation(string text)
        {
            var document = Sources.GetDocument(text, true);
            var visitor = new InstrumentationSyntaxVisitor(document, await document.GetSemanticModelAsync());
            var rewritten = new InstrumentationSyntaxRewriter(
                visitor.Augmentations.Data.Keys,
                 visitor.VariableLocations ,
                 visitor.Augmentations 
                );
            return rewritten.ApplyToTree(document.GetSyntaxTreeAsync().Result).GetText().ToString().EnforceLF();
        }
    }
}
