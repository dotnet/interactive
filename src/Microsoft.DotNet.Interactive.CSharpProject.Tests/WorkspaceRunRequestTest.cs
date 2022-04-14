// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.DotNet.Interactive.CSharpProject.Tests
{
    public class WorkspaceRunRequestTest
    {
        [Fact]
        public void Can_parse_workspace_without_files()
        {
            var request = JsonConvert.DeserializeObject<Workspace>(@"{ workspaceType: ""console"", buffers: [{content: ""code"", id:""test"", position: 12}] }");
            request.Buffers.Should().NotBeNullOrEmpty();
            request.Files.Should().BeNullOrEmpty();
            request.WorkspaceType.Should().Be("console");
            request.Buffers.FirstOrDefault(b => b.Id.FileName == "test").Should().NotBeNull();
        }

        [Fact]
        public void Can_parse_workspace_with_files()
        {
            var request = JsonConvert.DeserializeObject<Workspace>(@"{ workspaceType: ""console"", buffers: [{content: ""code"", id:""test"", position: 12}], files:[{name: ""filedOne.cs"", text:""some value""}] }");
            request.Buffers.Should().NotBeNullOrEmpty();
            request.Files.Should().NotBeNullOrEmpty();
            request.WorkspaceType.Should().Be("console");
            request.Buffers.FirstOrDefault(b => b.Id.FileName == "test").Should().NotBeNull();
            request.Files.FirstOrDefault(b => b.Name == "filedOne.cs").Should().NotBeNull();
        }

        [Fact]
        public void Can_parse_workspace_with_usings()
        {
            var request = JsonConvert.DeserializeObject<Workspace>(@"{ usings: [""using System1;"", ""using System2;""], workspaceType: ""console"", buffers: [{content: ""code"", id:""test"", position: 12}], files:[{name: ""filedOne.cs"", text:""some value""}] }");
            request.Buffers.Should().NotBeNullOrEmpty();
            request.Files.Should().NotBeNullOrEmpty();
            request.WorkspaceType.Should().Be("console");
            request.Buffers.FirstOrDefault(b => b.Id.FileName == "test").Should().NotBeNull();
            request.Files.FirstOrDefault(b => b.Name == "filedOne.cs").Should().NotBeNull();
            request.Usings.Should().BeEquivalentTo("using System1;", "using System2;");
        }
    }
}