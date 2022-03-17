// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using File = System.IO.File;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests.Instrumentation
{

    internal class Sources
    {
        internal static Document GetDocument(string source, bool forceLineEndings = false)
        {
            if (forceLineEndings) source = source.Replace("\r\n", "\n");
            var syntaxTree = CSharpSyntaxTree.ParseText(source);

            var workspace = new AdhocWorkspace();
            var solution = workspace.CurrentSolution;
            var project = solution.AddProject("testProject", "test.dll", LanguageNames.CSharp);
            var document = project.AddDocument("document.cs", syntaxTree.GetRoot());

            MetadataReference mscorlib = MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location);
            MetadataReference console = MetadataReference.CreateFromFile(typeof(Console).GetTypeInfo().Assembly.Location);
            MetadataReference runtime = MetadataReference.CreateFromFile(typeof(FileAttributes).GetTypeInfo().Assembly.Location);
            MetadataReference linq = MetadataReference.CreateFromFile(typeof(Enumerable).GetTypeInfo().Assembly.Location);
            MetadataReference io = MetadataReference.CreateFromFile(typeof(File).GetTypeInfo().Assembly.Location);

            var projectWithReferences = document.Project.WithMetadataReferences(
                new[]
                {
                    mscorlib,
                    console,
                    runtime,
                    linq,
                    io
                });

            //var compilation = CSharpCompilation.Create("Program", new[] { syntaxTree }, new[] { mscorlib, console, runtime, linq, io });
            //compilation.

            // Todo return a document instead of a compilation
            return projectWithReferences.GetDocument(document.Id);
        }

        internal static readonly string empty = @"
using System;
namespace RoslynRecorder
{
    class Program
    {
        static void Main()
        {
        }
    }
}".EnforceLF();

        internal static readonly string simple = @"
using System;
namespace RoslynRecorder
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine(""Entry Point"");
        }
    }
}".EnforceLF();

        internal static readonly string withMultipleRegion = @"
 using System;
namespace RoslynRecorder
{
    class Program
    {
        static void Main()
        {
#region test
            Console.WriteLine(""Entry Point"");
#endregion
        }
#region notthis
    }
#endregion
}   
".EnforceLF();

        internal static readonly string withLocalsAndParams = @"
using System;
namespace RoslynRecorder
{
    class Program
    {
        static void Main(string[] args)
        {
            int a = 0;
            Console.WriteLine(""Entry Point"");
        }
    }
}".EnforceLF();
        internal static readonly string withLocalParamsAndRegion = @"
using System;
namespace RoslynRecorder
{
    class Program
    {
        static void Main(string[] args)
        {
#region test
            int a = 0;
            Console.WriteLine(""Entry Point"");
#endregion
        }
    }
}".EnforceLF();

        internal static readonly string withNonAssignedLocals = @"
using System;
namespace RoslynRecorder
{
    class Program
    {
        static void Main()
        {
            string s;
            int a = 0;
            Console.WriteLine(""Entry Point"");
            s = ""now has a value"";
            a = 3;
        }
    }
}".EnforceLF();

        internal static readonly string withStaticAndNonStaticField = @"
using System;
namespace RoslynRecorder
{
    class Program
    {
        static int a = 0;
        int b = 0;

        static void Main()
        {
            Console.WriteLine(""Entry Point"");
        }

        void InstanceMain()
        {
            Console.WriteLine(""Instance"");
        }
    }
}".EnforceLF();

        internal static readonly string withMultipleMethodsAndComplexLayout = @"
using System;
namespace RoslynRecorder
{
    class Program
    {
        static int a = 0;
        int b = 0;

        static void Main(string[] args)
        {
            Console.WriteLine(""Entry Point"");
            var p = new Program();
            p.InstanceMain();
            
            int j = 0;
            int k;
            for(int i = 0; i < 10; i++)
            {
                j++;
            }
            foreach(var number in Enumerable.Range(1, 10))
            {
                Console.WriteLine(number);
            }
        }

        void InstanceMain()
        {
            Console.WriteLine(""Instance"");
        }
    }
}".EnforceLF();

        internal static readonly string withDynamic = @"
using System;
namespace RoslynRecorder
{
    class Program
    {
        static void Main(string[] args)
        {
            dynamic a = 0;
            Console.WriteLine(""Entry Point"");
        }
    }
}".EnforceLF();

    }
}

