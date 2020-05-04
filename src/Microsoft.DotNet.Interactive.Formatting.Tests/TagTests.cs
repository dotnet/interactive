// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class TagTests : FormatterTestBase
    {
        [Fact]
        public void Name_property_is_set_by_constructor()
        {
            var tag = new Tag("label");

            tag.Name.Should().Be("label");
        }

        [Fact]
        public void ToString_renders_start_tag_when_there_is_no_content()
        {
            var p = new Tag("p");

            p.ToString().Should().Contain("<p>");
        }

        [Fact]
        public void ToString_renders_end_tag_when_there_is_no_content()
        {
            var p = new Tag("p");

            p.ToString().Should().Contain("<p></p>");
        }

        [Fact]
        public void ToString_renders_text_content()
        {
            var p = new Tag("p", "content");

            p.ToString().Should().Be("<p>content</p>");
        }

        [Fact]
        public void ToString_renders_Action_content()
        {
            var p = new Tag("p", w => w.Write("content"));

            p.ToString().Should().Be("<p>content</p>");
        }

        [Fact]
        public void Append_appends_content_to_existing_Tag_content()
        {
            var tag = new Tag("div").Containing("initial content");

            var html = tag
                       .Append(new Tag("div").Containing("more content"))
                       .ToString();

            html.Should().Be("<div>initial content<div>more content</div></div>");
        }

        [Fact]
        public void Append_array_overload_appends_content_to_existing_Tag_content()
        {
            var tag = new Tag("div").Containing("initial content");

            var html = tag
                       .Append(new Tag("div").Containing("more"), new Tag("div").Containing("content"))
                       .ToString();

            html.Should().Be("<div>initial content<div>more</div><div>content</div></div>");
        }

        [Fact]
        public void Append_appends_to_tag_with_no_content()
        {
            var tag = "div".Tag().Append("img".Tag().SelfClosing()).ToString();

            tag.Should().Be("<div><img /></div>");
        }

        [Fact]
        public void Prepend_prepends_to_tag_with_no_content()
        {
            var tag = "div".Tag().Prepend("img".Tag().SelfClosing()).ToString();

            tag.Should().Be("<div><img /></div>");
        }

        [Fact]
        public void AppendTo_appends_content_to_target_Tag_content()
        {
            var tag = new Tag("div").Containing("initial content");
            new Tag("div").Containing("more content").AppendTo(tag);
            var html = tag.ToString();

            html.Should().Be("<div>initial content<div>more content</div></div>");
        }

        [Fact]
        public void AppendTo_appends_content_to_target_Tag_that_has_no_content()
        {
            var tag = "div".Tag();
            new Tag("div").Containing("some content").AppendTo(tag);
            var html = tag.ToString();

            html.Should().Be("<div><div>some content</div></div>");
        }

        [Fact]
        public void PrependTo_prepends_content_to_target_Tag_that_has_no_content()
        {
            var tag = "div".Tag();
            new Tag("div").Containing("some content").PrependTo(tag);
            var html = tag.ToString();

            html.Should().Be("<div><div>some content</div></div>");
        }

        [Fact]
        public void Append_returns_the_same_tag()
        {
            var tag = new Tag("div");
            tag.Append("a".Tag()).Should().Be(tag);
        }

        [Fact]
        public void AppendTo_returns_the_same_tag()
        {
            var tag = new Tag("div");
            tag.AppendTo("a".Tag()).Should().Be(tag);
        }

        [Fact]
        public void AppendTo_returns_unmodified_tag()
        {
            var target = "div".Tag();
            var before = "a".Tag();

            var after = before.AppendTo(target);

            after.ToString().Should().Be(before.ToString());
        }

        [Fact]
        public void Append_appends_many_tags_into_one()
        {
            var tag = "ul".Tag().Append("li".Tag(), "li".Tag());

            var html = tag.ToString();

            html.Should().Be("<ul><li></li><li></li></ul>");
        }

        [Fact]
        public void Prepend_returns_the_same_tag()
        {
            var tag = new Tag("div");

            tag.Prepend("a".Tag()).Should().Be(tag);
        }

        [Fact]
        public void PrependTo_returns_the_same_tag()
        {
            var tag = new Tag("div");

            tag.PrependTo("a".Tag()).Should().Be(tag);
        }

        [Fact]
        public void PrependTo_returns_unmodified_tag()
        {
            var target = "div".Tag();
            var before = "a".Tag();

            var after = before.PrependTo(target);

            after.ToString().Should().Be(before.ToString());
        }

        [Fact]
        public void WrapInner_returns_the_same_tag()
        {
            var tag = new Tag("div");
            tag.WrapInner("a".Tag()).Should().Be(tag);
        }

        [Fact]
        public void Prepend_prepends_content_to_existing_Tag_content()
        {
            var tag = new Tag("div").Containing("initial content");

            var after = tag
                        .Prepend(new Tag("div").Containing("more content"))
                        .ToString();

            after.Should().Be("<div><div>more content</div>initial content</div>");
        }

        [Fact]
        public void WrapInner_wraps_inner_content_in_specified_tag()
        {
            var tag = new Tag("div").Containing("initial content");

            var after = tag
                        .WrapInner("a".Tag().WithAttributes("href", "#"))
                        .ToString();

            after.Should().Be("<div><a href=\"#\">initial content</a></div>");
        }

        [Fact]
        public void MergedAttributes_produces_a_tag_having_the_union_of_attributes_differing_by_name()
        {
            var tag = new Tag("div");
            tag.MergeAttributes(new HtmlAttributes { { "class", "error" } });
            tag.MergeAttributes(new HtmlAttributes { { "style", "display:block" } });

            var html = tag.ToString();

            html.Should().Contain("<div class=\"error\" style=\"display:block\"></div>");
        }

        [Fact]
        public void WithAttributes_produces_a_tag_having_the_union_of_attributes_differing_by_name()
        {
            var tag = new Tag("div")
                      .WithAttributes(new HtmlAttributes { { "class", "error" } })
                      .WithAttributes(new HtmlAttributes { { "style", "display:block" } });

            var html = tag.ToString();

            html.Should().Contain("<div class=\"error\" style=\"display:block\"></div>");
        }

        [Fact]
        public void When_called_more_than_once_with_attributes_of_the_same_name_MergeAttributes_does_not_overwrite_by_default()
        {
            var tag = new Tag("div");
            tag.MergeAttributes(new HtmlAttributes { { "class", "error" } });
            tag.MergeAttributes(new HtmlAttributes { { "class", "alert" } });

            var html = tag.ToString();

            html.Should().Contain("<div class=\"error\"></div>");
        }

        [Fact]
        public void When_called_more_than_once_with_attributes_of_the_same_name_WithAttributes_overwrites_by_default()
        {
            var tag = new Tag("div")
                      .WithAttributes(new HtmlAttributes { { "class", "error" } })
                      .WithAttributes(new HtmlAttributes { { "class", "alert" } });

            var html = tag.ToString();

            html.Should().Contain("<div class=\"alert\"></div>");
        }

        [Fact]
        public void When_called_more_than_once_with_attributes_of_the_same_name_MergeAttributes_and_overwrite_is_set_to_true_it_overwrites()
        {
            var tag = new Tag("div");
            tag.MergeAttributes(new HtmlAttributes { { "class", "error" } });
            tag.MergeAttributes(new HtmlAttributes { { "class", "alert" } }, true);

            var html = tag.ToString();

            html.Should().Contain("<div class=\"alert\"></div>");
        }

        [Fact]
        public void IsSelfClosing_renders_selfclosing_when_no_content()
        {
            var tag = "pizza".Tag();
            tag.IsSelfClosing = true;

            var after = tag.ToString();

            after.Should().Be("<pizza />");
        }

        [Fact]
        public void IsSelfClosing_renders_full_tag_when_content()
        {
            var tag = new Tag("pizza").Containing("some content");
            tag.IsSelfClosing = true;

            var after = tag.ToString();
            after.Should().Be("<pizza>some content</pizza>");
        }

        [Fact]
        public void SelfClosing_extension_sets_IsSelfClosing_property()
        {
            var tag = "input".Tag();

            tag.IsSelfClosing.Should().BeFalse();

            tag.SelfClosing();

            tag.IsSelfClosing.Should().BeTrue();
        }
    }
}