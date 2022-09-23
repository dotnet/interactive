using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.Documents;
using Microsoft.DotNet.Interactive.Documents.Jupyter;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Tests.Utility;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class ImportNotebookTests
    {
        [Fact]
        public async Task It_imports_and_runs_ipynb()
        {
            using var kernel = new CompositeKernel { 
                new CSharpKernel()
            }
                .UseImportMagicCommand();

            var document = new InteractiveDocument();
            document.Add(new InteractiveDocumentElement
            {
                Contents = "1+1",
                KernelName = "csharp"
            });

            string filePath = @"c:\temp\testnotebook.ipynb";

            File.WriteAllText(filePath, Notebook.SerializeToJupyter(document));

            using var events = kernel.KernelEvents.ToSubscribedList();

            await kernel.SubmitCodeAsync($"#!import {filePath}");

            events.Should()
                .ContainSingle<ReturnValueProduced>().Which.Value.Should().Be(2);

            //throw new NotImplementedException();
        }

        //both ipynb and dib
        //ignore markdown cells
    }
}