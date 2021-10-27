using FluentAssertions;
using Microsoft.AspNetCore.Html;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.DotNet.Interactive.Tests
{
    public class DataExplorerTests
    {
        [Fact]
        public async Task when_there_is_single_dataexplorer_return_it_as_default()
        {
            DataExplorer<string>.Register<StringDataExplorer>();
            var dataExplorer = DataExplorer.CreateDefault("hello world");
            dataExplorer.Should().BeOfType<StringDataExplorer>();
        }


        [Fact]
        public async Task can_create_specific_dataexplorer_for_a_datatype()
        {
            DataExplorer<string>.Register<StringDataExplorer>();
            DataExplorer<string>.Register<AdvancedStringDataExplorer>();
            var dataExplorer = DataExplorer.Create("AdvancedStringDataExplorer", "hello world");
            dataExplorer.Should().BeOfType<AdvancedStringDataExplorer>();
        }

        [Fact]
        public async Task can_specify_default_dataexplorer_for_a_datatype()
        {
            DataExplorer<string>.Register<StringDataExplorer>();
            DataExplorer<string>.Register<AdvancedStringDataExplorer>();
            DataExplorer.SetDefault<string>(nameof(AdvancedStringDataExplorer));
            var dataExplorer = DataExplorer.CreateDefault("hello world");
            dataExplorer.Should().BeOfType<AdvancedStringDataExplorer>();
        }


        private class StringDataExplorer : DataExplorer<string>
        {
            public StringDataExplorer(string data) : base(data)
            {
            }

            protected override IHtmlContent ToHtml()
            {
                return new HtmlString(Data);
            }
        }

        private class AdvancedStringDataExplorer : DataExplorer<string>
        {
            public AdvancedStringDataExplorer(string data) : base(data)
            {
            }

            protected override IHtmlContent ToHtml()
            {
                return new HtmlString(Data);
            }
        }
    }

}
