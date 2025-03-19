// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Numerics;
using FluentAssertions;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public partial class HtmlFormatterTests
{
    [TestClass]
    public class PreformatPlainText : FormatterTestBase
    {
        [TestMethod]
        public void It_can_format_a_String_with_class()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(string));

            var writer = new StringWriter();

            var instance = @"this
is a 
   multiline<>
string";

            formatter.Format(instance, writer);

            writer.ToString().RemoveStyleElement()
                  .Should()
                  .BeEquivalentHtmlTo(
                      $"{Tags.PlainTextBegin}{instance.HtmlEncode()}{Tags.PlainTextEnd}");
        }

        [TestMethod]
        public void HtmlFormatter_returns_plain_for_decimal()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor<decimal>();

            var d = 10.123m.ToDisplayString(formatter).RemoveStyleElement();

            d.Should().Be($"{Tags.PlainTextBegin}10.123{Tags.PlainTextEnd}");
        }


        [TestMethod]
        public void HtmlFormatter_returns_plain_for_BigInteger()
        {
            var formatter = HtmlFormatter.GetPreferredFormatterFor(typeof(BigInteger));

            var writer = new StringWriter();

            var instance = BigInteger.Parse("78923589327589332402359");

            formatter.Format(instance, writer);

            var html = writer.ToString().RemoveStyleElement();

            html.Should()
                .Be($"{Tags.PlainTextBegin}78923589327589332402359{Tags.PlainTextEnd}");
        }
    }
}