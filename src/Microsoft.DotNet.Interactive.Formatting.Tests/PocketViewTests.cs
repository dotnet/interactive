// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using FluentAssertions;
using System.Linq;
using Microsoft.AspNetCore.Html;
using Xunit;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;
using static Microsoft.DotNet.Interactive.Formatting.Tests.Tags;
using Microsoft.DotNet.Interactive.Formatting.Tests.Utility;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public class PocketViewTests : FormatterTestBase
{
    [Fact]
    public void Outputs_nested_html_elements_with_attributes()
    {
        string output = html(
                body(
                    ul[@class: "sold-out", id: "cat-products"](
                        li(a("Scratching post")),
                        li(a[@class: "selected"]("Cat Fountain")))))
            .ToString();

        output
            .Should()
            .Be(
                @"<html><body><ul class=""sold-out"" id=""cat-products""><li><a>Scratching post</a></li><li><a class=""selected"">Cat Fountain</a></li></ul></body></html>");
    }

    [Fact]
    public void Comma_delimited_arguments_output_sibling_elements()
    {
        string output = ul(
            li("one"),
            li("two"),
            li("three")).ToString();

        output
            .Should()
            .Be("<ul><li>one</li><li>two</li><li>three</li></ul>");
    }

    [Fact]
    public void When_a_sibling_element_is_IEnumerable_it_is_enumerated()
    {
        string output = ul(Enumerable.Range(1, 3).Select(i => li(i))).ToString();

        output.RemoveStyleElement()
            .Should()
            .Be($"<ul><li>{PlainTextBegin}1{PlainTextEnd}</li><li>{PlainTextBegin}2{PlainTextEnd}</li><li>{PlainTextBegin}3{PlainTextEnd}</li></ul>");
    }

    [Fact]
    public void Tag_content_can_be_specified_using_a_string()
    {
        string output = div("foo").ToString();

        output.Should().Be("<div>foo</div>");
    }

    [Fact]
    public void A_tag_with_one_attribute_passed_as_a_hash()
    {
        string output = div[@class: "bar"]("foo").ToString();

        output.Should().Be("<div class=\"bar\">foo</div>");
    }

    [Fact]
    public void A_tag_with_two_attributes_passed_as_a_hash()
    {
        string output = div[@class: "bar", width: "100px"]("foo").ToString();

        output
            .Should().Be("<div class=\"bar\" width=\"100px\">foo</div>");
    }

    [Fact]
    public void A_nested_tag()
    {
        string output = div(a("foo")).ToString();

        output.Should().Be("<div><a>foo</a></div>");
    }

    [Fact]
    public void If_a_string_is_passed_as_tag_contents_it_is_encoded()
    {
        string output = div("this & that").ToString();

        output.Should().Be("<div>this &amp; that</div>");
    }

    [Fact]
    public void If_an_IHtmlContent_is_passed_as_tag_contents_it_is_not_reencoded()
    {
        string output = div("this &amp; that".ToHtmlContent()).ToString();

        output.Should().Be("<div>this &amp; that</div>");
    }

    [Fact]
    public void If_a_string_is_passed_as_attribute_contents_it_is_encoded()
    {
        string output = div[data: "this & that"]("hi").ToString();

        output.Should().Be("<div data=\"this &amp; that\">hi</div>");
    }

    [Fact]
    public void If_an_IHtmlContent_is_passed_as_attribute_contents_it_is_not_reencoded()
    {
        var htmlContent = "this &amp; that".ToHtmlContent();

        string output = div[data: htmlContent].ToString();

        output.Should().Be("<div data=\"this &amp; that\"></div>");
    }

    [Fact]
    public void A_transform_can_be_used_to_create_an_alias()
    {
        _.foolink = PocketView.Transform(
            (tag, model) =>
            {
                tag.Name = "a";
                tag.HtmlAttributes.Add("href", "http://foo.biz");
            });

        string output = _.foolink("click here!").ToString();

        output
            .Should()
            .Be("<a href=\"http://foo.biz\">click here!</a>");
    }

    [Fact]
    public void A_transform_can_be_used_to_create_an_expandable_tag()
    {
        _.textbox = PocketView.Transform(
            (tag, model) =>
            {
                tag.Name = "div";
                tag.Content = (context) =>
                {
                    context.Writer.Write(label[@for: model.name](model.name));
                    context.Writer.Write(input[name: model.name, type: "text", value: model.value]);
                };
            });

        string output = _.textbox(name: "FirstName", value: "Bob").ToString();

        output
            .Should().Be("<div><label for=\"FirstName\">FirstName</label><input name=\"FirstName\" type=\"text\" value=\"Bob\" /></div>");
    }

    [Fact]
    public void A_transform_can_be_used_to_add_attributes()
    {
        dynamic _ = new PocketView();
            
        _.div = PocketView.Transform(
            (tag, model) => tag.HtmlAttributes.Class("foo"));

        string output = _.div("hi").ToString();

        output.Should().Be("<div class=\"foo\">hi</div>");
    }

    [Fact]
    public void A_transform_merges_differently_named_attributes()
    {
        dynamic _ = new PocketView();

        _.div = PocketView.Transform(
            (tag, model) => tag.HtmlAttributes.Class("foo"));

        string output = _.div[style: "color:red"]("hi").ToString();

        output
            .Should().Be("<div style=\"color:red\" class=\"foo\">hi</div>");
    }

    [Fact]
    public void A_transform_overwrites_like_named_attributes()
    {
        dynamic _ = new PocketView();

        _.div = PocketView.Transform(
            (tag, model) => tag.HtmlAttributes["style"] = "color:yellow");

        string output = _.div[style: "color:red"]("hi").ToString();

        output
            .Should()
            .Be("<div style=\"color:yellow\">hi</div>");
    }

    [Fact]
    public void HtmlAttributes_can_be_used_for_attributes()
    {
        var attr = new HtmlAttributes();

        string output = section[attr.Class("info")]("some content").ToString();

        output.Should()
            .Be("<section class=\"info\">some content</section>");
    }

    [Fact]
    public void Input_elements_are_rendered_as_self_closing_when_no_content_is_passed_to_method_invocation()
    {
        string output = input[type: "button", value: "go"]().ToString();

        output.Should().Be("<input type=\"button\" value=\"go\" />");
    }

    [Fact]
    public void Input_elements_are_rendered_as_self_closing_when_calling_using_property_accessor()
    {
        string output = input[type: "button", value: "go"].ToString();

        output.Should().Be("<input type=\"button\" value=\"go\" />");
    }

    [Fact]
    public void HtmlAttributes_mixed_with_indexer_args_can_be_used_for_attributes()
    {
        var attr = new HtmlAttributes();

        string output = section[attr.Class("info"), style: "color:red"]("some content").ToString();

        output.Should().Be("<section class=\"info\" style=\"color:red\">some content</section>");
    }

    [Fact]
    public void Underscores_in_attribute_names_are_replaced_with_hyphens()
    {
        string output = input[data_bind: "some_element"]().ToString();

        output.Should().Be("<input data-bind=\"some_element\" />");
    }

    [Fact]
    public void attributes_can_be_written_without_a_value()
    {
        string output = details["open"](summary("heading"), p("some content")).ToString();

        output.Should().Be("<details open><summary>heading</summary><p>some content</p></details>");
    }

    [Fact]
    public void multiple_attributes_can_be_written_without_a_value()
    {
        string output = details["open", "disabled"](summary("heading"), p("some content")).ToString();

        output.Should().Be("<details open disabled><summary>heading</summary><p>some content</p></details>");
    }

    [Fact]
    public void Attributes_without_a_value_can_be_combined_with_named_attributes()
    {
        PocketView view = details["open", @class: "the-class"](summary("expand for more"), div("more"));

        var html = view.ToString();

        html.Should().Be("""
            <details open class="the-class">
                <summary>expand for more</summary>
                <div>more</div>
            </details>
            """.Crunch());
    }

    [Fact]
    public void Method_and_property_calls_return_the_same_result_when_no_attributes_are_present()
    {
        string property = _.foo.ToString();

        var method = _.foo().ToString();

        property.Should().Be(method);
    }

    [Fact]
    public void Method_and_property_calls_return_the_same_result_when_attributes_are_present()
    {
        string property = _.foo[bar: "baz"].ToString();
        string method = _.foo[bar: "baz"]().ToString();

        property.Should().Be(method);
    }

    [Fact]
    public void br_method_invocation_writes_a_self_closing_tag()
    {
        string output = br().ToString();

        output.Should().Be("<br />");
    }

    [Fact]
    public void br_property_invocation_writes_a_self_closing_tag()
    {
        string output = br.ToString();

        output.Should().Be("<br />");
    }

    public class Styling
    {
        [Fact]
        public void When_style_element_is_required_more_than_once_it_is_output_exactly_once_per_top_level_render()
        {
            var css = @"a.my-class {
  color: purple;
}";

            IEnumerable<IHtmlContent> ps = new[] { "apple", "banana", "cherry" }
                .Select(text =>
                {
                    PocketView content = p[@class: "my-class"](text);
                    content.AddDependency("required-styles", style(css));
                    return content;
                });

            string html = div(ps).ToString();

            html.Should().BeExceptingWhitespace($@"
<div>
    <p class=""my-class"">apple</p>
    <p class=""my-class"">banana</p>
    <p class=""my-class"">cherry</p>
</div>
<style>
{css}
</style>");
        }

        [Fact]
        public void Style_can_be_created_using_collection_initializer()
        {
            var style = new Style
            {
                { "apple", ("color", "red"), ("text-align", "center") }
            };

            style.ToString()
                .Should()
                .BeExceptingWhitespace(@"
<style>
apple {
  color: red;
  text-align: center;
}
</style>");
        }

        [Fact]
        public void Styles_aggregate_properties_when_add_is_called_repeatedly_using_the_same_selector()
        {
            var style = new Style();
            style.Add("apple", ("color", "red"));
            style.Add("apple", ("text-align", "center"));

            style.ToString()
                .Should()
                .BeExceptingWhitespace(@"
<style>
apple {
  color: red;
  text-align: center;
}
</style>");
        }
    }
}