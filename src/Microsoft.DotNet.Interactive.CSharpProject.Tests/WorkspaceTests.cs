// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.DotNet.Interactive.CSharpProject.MLS.Project;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public class WorkspaceTests
    {
        [Fact]
        public void I_can_extracts_viewPorts_when_files_declare_region()
        {
            var ws = new Workspace(files: new[]
            {
                new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramSingleRegion)
            });

            var viewPorts = ws.ExtractViewPorts();
            viewPorts.Should().NotBeEmpty();
            viewPorts.Select(p => p.BufferId.ToString()).Should().BeEquivalentTo("Program.cs@alpha");
        }

        [Fact]
        public void ViewPort_ids_must_be_uinique_within_a_file()
        {
            var ws = new Workspace(files: new[]
            {
                new ProjectFileContent("Program.cs", SourceCodeProvider.ConsoleProgramCollidingRegions)
            });

            Action extraction = () => ws.ExtractViewPorts().ToList();
            extraction.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ViewPort_ids_must_be_unique_inside_the_workspace()
        {
            var ws = new Workspace(files: new[]
            {
                new ProjectFileContent("ProgramA.cs", SourceCodeProvider.ConsoleProgramSingleRegion),
                new ProjectFileContent("ProgramB.cs", SourceCodeProvider.ConsoleProgramSingleRegion)
            });

            Action extraction = () => ws.ExtractViewPorts();
            extraction.Should().NotThrow<InvalidOperationException>();
        }
        
        [Fact]
        public void ViewPort_extraction_fails_with_null_workspace()
        {
            Action extraction = () => ((Workspace)null).ExtractViewPorts().ToList();
            extraction.Should().Throw<ArgumentNullException>();
        }

    }
}