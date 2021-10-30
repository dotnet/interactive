// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Assent;

using FluentAssertions;

using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.Formatting.TabularData;
using Xunit;

namespace Microsoft.DotNet.Interactive.ExtensionLab.Tests
{
    public class NteractKernelExtensionTests : IDisposable
    {
        private readonly Configuration _configuration;

        public NteractKernelExtensionTests()
        {
            _configuration = new Configuration()
                .SetInteractive(Debugger.IsAttached)
                .UsingExtension("json");
        }

        [Fact]
        public async Task it_configures_preferred_mimeTypes()
        {
            using var kernel = new CompositeKernel();

            var kernelExtension = new NteractKernelExtension();

            await kernelExtension.OnLoadAsync(kernel);

            var mimetypes = Formatter.GetPreferredMimeTypesFor(typeof(NteractDataExplorer));

            mimetypes
                .Should().BeEquivalentTo(HtmlFormatter.MimeType, TabularDataResourceFormatter.MimeType);
        }


        [Fact]
        public async Task it_registers_html_formatter()
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


            var formattedValue = data.ExploreWithNteract().ToDisplayString(HtmlFormatter.MimeType);
            formattedValue.Should().Contain("configureRequireFromExtension('nteract','1.0.0')(['nteract/nteractapi'], (nteract) => {");
        }

        [Fact]
        public async Task it_registers_tabularResource_formatter()
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


            var formattedValue = data.ExploreWithNteract().ToDisplayString(TabularDataResourceFormatter.MimeType);
            formattedValue.Should().Contain("\"profile\": \"tabular-data-resource\"");
        }

        [Fact]
        public async Task it_is_formatted_as_multiple_mimeTypes()
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


            var formattedValues = FormattedValue.FromObject( data.ExploreWithNteract());
            formattedValues.Select(fv => fv.MimeType)
                .Should()
                .BeEquivalentTo(HtmlFormatter.MimeType, TabularDataResourceFormatter.MimeType);
        }

        [Fact]
        public async Task widget_code_generation_is_not_broken()
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


            var html = data.ExploreWithNteract().ToDisplayString(HtmlFormatter.MimeType);

           this.Assent(html.FixedGuid());
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

            var formatted = data.ExploreWithNteract().ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should().Contain("configureRequireFromExtension");
        }

        [Fact]
        public async Task it_checks_and_load_require()
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


            var explorer = data.ExploreWithNteract();
            explorer.LibraryUri = new Uri("https://a.cdn.url/script.js");
            var formatted = explorer.ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should()
                .Contain("if ((typeof(require) !==  typeof(Function)) || (typeof(require.config) !== typeof(Function)))")
                .And
                .Contain("require_script.onload = function()");
        }

        [Fact]
        public async Task it_can_loads_script_from_uri()
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

            var explorer = data.ExploreWithNteract();
            explorer.LibraryUri = new Uri("https://a.cdn.url/script.js");
            var formatted = explorer.ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should().Contain("require.config(");
        }

        [Fact]
        public async Task it_can_loads_script_from_uri_and_specify_context()
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


            var explorer = data.ExploreWithNteract();
            explorer.LibraryUri = new Uri("https://a.cdn.url/script.js");
            explorer.LibraryVersion = "2.2.2";
            var formatted = explorer.ToDisplayString(HtmlFormatter.MimeType);
            formatted.Should().Contain("'context': '2.2.2'");
        }

        [Fact]
        public async Task uri_is_quoted()
        {
            using var kernel = new CompositeKernel();

            var kernelExtension = new NteractKernelExtension();
            await kernelExtension.OnLoadAsync(kernel);
            kernel.UseNteractDataExplorer("https://a.cdn.url/script.js");

            var data = new[]
            {
                new {Type="orange", Price=1.2},
                new {Type="apple" , Price=1.3},
                new {Type="grape" , Price=1.4}
            };

            var explorer = data.ExploreWithNteract();
            explorer.LibraryUri = new Uri("https://a.cdn.url/script.js");
            var formatted = explorer.ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should().Contain("'https://a.cdn.url/script'");
        }

        [Fact]
        public async Task uri_extension_is_removed()
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


            var explorer = data.ExploreWithNteract();
            explorer.LibraryUri = new Uri("https://a.cdn.url/script.js");
            var formatted = explorer.ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should()
                .Contain("'https://a.cdn.url/script'")
                .And
                .NotContain("'https://a.cdn.url/script.js'");
        }

        [Fact]
        public async Task can_specify_cacheBuster()
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


            var explorer = data.ExploreWithNteract();
            explorer.LibraryUri = new Uri("https://a.cdn.url/script.js");
            explorer.CacheBuster = "XYZ";
            var formatted = explorer.ToDisplayString(HtmlFormatter.MimeType);

            formatted.Should().Contain("'urlArgs': 'cacheBuster=XYZ'");
        }

        public void Dispose()
        {
            Formatter.ResetToDefault();
            NteractDataExplorer.ResetDefaultConfiguration();
        }
    }
}