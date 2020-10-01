// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Assent;

using FluentAssertions;

using Microsoft.DotNet.Interactive.Formatting;

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public class NteractKernelExtensionTests : IDisposable
    {
        private readonly Configuration _configuration;

        public NteractKernelExtensionTests(ITestOutputHelper output)
        {
            _configuration = new Configuration()
                .SetInteractive(Debugger.IsAttached)
                .UsingExtension("json");
        }

        [Fact]
        public async Task it_registers_formatters()
        {
            using var kernel = new CompositeKernel();

            var kernelExtension = new NteractKernelExtension();

            await kernelExtension.OnLoadAsync(kernel);

            var data = new[]
            {
                new {Type="orange", Price=1.2},
                new {Type="apple" , Price=1.3},
                new {Type="grape" , Price=1.4}
            };


            var formatted = data.ToTabularJsonString().ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should().Contain("getExtensionRequire('nteract','1.0.0')(['nteract/index'], (nteract) => {");
        }

        [Fact]
        public async Task it_can_load_script_from_the_extension()
        {
            using var kernel = new CompositeKernel();

            var kernelExtension = new NteractKernelExtension();

            await kernelExtension.OnLoadAsync(kernel);

            var data = new[]
            {
                new {Type="orange", Price=1.2},
                new {Type="apple" , Price=1.3},
                new {Type="grape" , Price=1.4}
            };


            var formatted = data.ToTabularJsonString().ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should().Contain("getExtensionRequire");
        }

        [Fact]
        public async Task it_can_loads_script_from_uri()
        {
            using var kernel = new CompositeKernel();

            var kernelExtension = new NteractKernelExtension();
            DataExplorerExtensions.Settings.UseUri("https://a.cdn.url/script.js");
            await kernelExtension.OnLoadAsync(kernel);

            var data = new[]
            {
                new {Type="orange", Price=1.2},
                new {Type="apple" , Price=1.3},
                new {Type="grape" , Price=1.4}
            };


            var formatted = data.ToTabularJsonString().ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should().Contain("require.config(");
        }

        [Fact]
        public async Task it_can_loads_script_from_uri_and_specify_context()
        {
            using var kernel = new CompositeKernel();

            var kernelExtension = new NteractKernelExtension();
            DataExplorerExtensions.Settings.UseUri("https://a.cdn.url/script.js", "2.2.2");
            await kernelExtension.OnLoadAsync(kernel);

            var data = new[]
            {
                new {Type="orange", Price=1.2},
                new {Type="apple" , Price=1.3},
                new {Type="grape" , Price=1.4}
            };


            var formatted = data.ToTabularJsonString().ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should().Contain("'context': '2.2.2'");
        }

        [Fact]
        public async Task uri_is_quoted()
        {
            using var kernel = new CompositeKernel();

            var kernelExtension = new NteractKernelExtension();
            DataExplorerExtensions.Settings.UseUri("https://a.cdn.url/script.js");
            await kernelExtension.OnLoadAsync(kernel);

            var data = new[]
            {
                new {Type="orange", Price=1.2},
                new {Type="apple" , Price=1.3},
                new {Type="grape" , Price=1.4}
            };


            var formatted = data.ToTabularJsonString().ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should().Contain("'https://a.cdn.url/script'");
        }

        [Fact]
        public async Task uri_extension_is_removed()
        {
            using var kernel = new CompositeKernel();

            var kernelExtension = new NteractKernelExtension();
            DataExplorerExtensions.Settings.UseUri("https://a.cdn.url/script.js");
            await kernelExtension.OnLoadAsync(kernel);

            var data = new[]
            {
                new {Type="orange", Price=1.2},
                new {Type="apple" , Price=1.3},
                new {Type="grape" , Price=1.4}
            };


            var formatted = data.ToTabularJsonString().ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should().NotContain("'https://a.cdn.url/script.js'");
        }

        [Fact]
        public async Task can_specify_cacheBuster()
        {
            using var kernel = new CompositeKernel();

            var kernelExtension = new NteractKernelExtension();
            DataExplorerExtensions.Settings.UseUri("https://a.cdn.url/script.js", cacheBuster:"XYZ");
            await kernelExtension.OnLoadAsync(kernel);

            var data = new[]
            {
                new {Type="orange", Price=1.2},
                new {Type="apple" , Price=1.3},
                new {Type="grape" , Price=1.4}
            };


            var formatted = data.ToTabularJsonString().ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should().Contain("'urlArgs': 'cacheBuster=XYZ'");
        }

        public void Dispose()
        {
            DataExplorerExtensions.Settings.RestoreDefault();
            Formatter.ResetToDefault();
        }
    }
}