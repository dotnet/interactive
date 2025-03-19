// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Tests;

[TestClass]
public class DataExplorerTests
{
    [TestMethod]
    public void when_there_is_single_DataExplorer_return_it_as_default()
    {
        DataExplorer<string>.Register<StringDataExplorer>();
        var dataExplorer = DataExplorer.CreateDefault("hello world");
        dataExplorer.Should().BeOfType<StringDataExplorer>();
    }


    [TestMethod]
    public void can_create_specific_DataExplorer_for_a_data_type()
    {
        DataExplorer<string>.Register<StringDataExplorer>();
        DataExplorer<string>.Register<AdvancedStringDataExplorer>();
        var dataExplorer = DataExplorer.Create("AdvancedStringDataExplorer", "hello world");
        dataExplorer.Should().BeOfType<AdvancedStringDataExplorer>();
    }

    [TestMethod]
    public void can_specify_default_DataExplorer_for_a_data_type()
    {
        DataExplorer<string>.Register<StringDataExplorer>();
        DataExplorer<string>.Register<AdvancedStringDataExplorer>();
        DataExplorer.SetDefault<string, AdvancedStringDataExplorer>();
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