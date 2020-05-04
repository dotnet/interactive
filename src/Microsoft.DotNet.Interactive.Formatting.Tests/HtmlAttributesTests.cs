// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Html;
using Xunit;

namespace Microsoft.DotNet.Interactive.Formatting.Tests
{
    public class HtmlAttributesTests : FormatterTestBase
    {
        [Fact]
        public void When_object_constructor_overload_is_passed_a_dictionary_it_initializes_correctly()
        {
            var attributes =
                new HtmlAttributes(
                    new Dictionary<string, object>
                    {
                        { "class", "required" },
                        { "style", "display:none" }
                    });

            var html = attributes.ToString();

            html.Should()
                .Be("class=\"required\" style=\"display:none\"", html);
        }

        [Fact]
        public void When_object_constructor_overload_is_passed_a_HtmlAttributes_it_initializes_correctly()
        {
            var attributes =
                new HtmlAttributes(
                    new HtmlAttributes
                    {
                        { "class", "required" },
                        { "style", "display:none" }
                    });

            var html = attributes.ToString();

            html.Should()
                .Be("class=\"required\" style=\"display:none\"");
        }

        [Fact]
        public virtual void Dictionary_Add_adds_items_to_dictionary()
        {
            var attributes = new HtmlAttributes();

            attributes.Add("foo", "bar");

            var value = attributes["foo"].ToString();

            value.Should().Be("bar");
        }

        [Fact]
        public virtual void Collection_initializer_adds_items_to_dictionary()
        {
            var attributes = new HtmlAttributes { { "foo", "bar" } };

            var value = attributes["foo"].ToString();

            value.Should().Be("bar");
        }

        [Fact]
        public virtual void When_HtmlAttributes_is_empty_ToString_returns_empty_string()
        {
            var attributes = new HtmlAttributes();

            var value = attributes.ToString();

            value.Should().BeEmpty();
        }

        [Fact]
        public virtual void ToString_returns_correct_value_for_single_dictionary_entry()
        {
            var attributes = new HtmlAttributes { { "foo", "bar" } };

            attributes.ToString().Should().Be("foo=\"bar\"");
        }

        [Fact]
        public virtual void ToString_returns_correct_value_for_multiple_dictionary_entries()
        {
            var attributes = new HtmlAttributes
            {
                { "one", "1" },
                { "two", "2" },
            };

            attributes.ToString().Should().Be("one=\"1\" two=\"2\"");
        }

        [Fact]
        public virtual void Dynamic_assignment_adds_items_to_dictionary()
        {
            dynamic attributes = new HtmlAttributes();

            attributes.foo = "bar";

            string value = attributes["foo"];

            value.Should().Be("bar");
        }

        [Fact]
        public virtual void Dictionary_items_can_be_dynamically_retrieved()
        {
            dynamic attributes = new HtmlAttributes { { "foo", "bar" } };

            string foo = attributes.foo;

            foo.Should().Be("bar");
        }

        [Fact]
        public virtual void Get_keys_returns_dynamically_assigned_keys()
        {
            dynamic attributes = new HtmlAttributes();
            attributes.one = 1;
            attributes.two = 2;
            attributes.three = 3;
            attributes.four = 4;
            attributes.five = 5;

            ((IDictionary<string, object>) attributes).Keys.Count.Should().Be(5);
        }

        [Fact]
        public virtual void Classes_are_aggregated_by_AddCssClass()
        {
            var attributes = new HtmlAttributes();

            attributes.AddCssClass("bar");
            attributes.AddCssClass("foo");

            attributes["class"].Should().Be("foo bar");
        }

        [Fact]
        public virtual void When_multiple_Add_calls_use_same_key_it_throws()
        {
            var attributes = new HtmlAttributes();

            attributes.Add("class", "one");

            Assert.Throws<ArgumentException>(() => attributes.Add("class", "two"));
        }

        [Fact]
        public virtual void Classes_are_overwritten_during_set_operations()
        {
            var attributes = new HtmlAttributes();

            attributes.Add("class", "one");
            attributes["class"] = "two";

            attributes["class"].Should().Be("two");
        }

        [Fact]
        public virtual void Classes_are_overwritten_during_dynamic_set_operations()
        {
            dynamic attributes = new HtmlAttributes();

            attributes.Add("class", "one");
            attributes.@class = "two";

            string @class = attributes.@class;

            @class.Should().Be("two");
        }

        [Fact]
        public virtual void Can_remove_dynamically_assigned_value()
        {
            var attributes = new HtmlAttributes();

            ((dynamic) attributes).foo = "bar";

            attributes.Remove("foo");

            attributes.Count.Should().Be(0);
        }

        [Fact]
        public virtual void ContainsKey_is_true_for_dynamically_assigned_property()
        {
            var attributes = new HtmlAttributes();

            ((dynamic) attributes).foo = "bar";

            attributes.ContainsKey("foo").Should().BeTrue();
        }

        [Fact]
        public virtual void Can_convert_arbitrary_named_parameters_into_attributes()
        {
            dynamic attributes = new HtmlAttributes();

            attributes.MergeWith(@class: "required", style: "display:block");

            string output = attributes.ToString();

            output.Should()
                  .Contain("class=\"required\" style=\"display:block\"");
        }

        [Fact]
        public void Attribute_values_containing_double_quotes_are_attribute_encoded()
        {
            dynamic attributes = new HtmlAttributes();
            var quote = "\"Reality\" is the only word in the English language that should always be used in quotes.";

            attributes.quote = quote;

            string value = attributes.ToString();

            value
                .Should()
                .Be($"quote=\"{quote.HtmlAttributeEncode()}\"");
        }

        [Fact]
        public void Data_attributes_containing_primitive_values_are_properly_quoted()
        {
            var attr = new HtmlAttributes();

            attr.Data("string", "name");
            attr.Data("int", 123);
            attr.Data("double", 1.23);

            attr.ToString().Should().Contain("data-int='123'");
            attr.ToString().Should().Contain("data-double='1.23'");
            attr.ToString().Should().Contain("data-string=\"name\"");
        }

        [Fact]
        public void Data_attributes_containing_IHtmlContent_are_not_reencoded()
        {
            var attr = new HtmlAttributes();

            attr.Data("bind", new { name = "Felix" }.SerializeToJson());

            attr.ToString()
                .Should()
                .Be("data-bind='{\"name\":\"Felix\"}'");
        }

        [Fact]
        public void Data_attributes_containing_json_can_be_added()
        {
            var attributes = new HtmlAttributes();

            attributes.Data("data-widget", new { Id = 789, Name = "The Stanley", Price = 24.99m });

            attributes.ToString()
                      .Should()
                      .Be("data-widget='{\"Id\":789,\"Name\":\"The Stanley\",\"Price\":24.99}'");
        }

        [Fact]
        public void Data_prepends_the_word_data_if_specified_attribute_name_does_not_start_with_the_word_data()
        {
            var attributes = new HtmlAttributes();

            attributes.Data("widget", new { Id = 789, Name = "The Stanley", Price = 24.99m });

            attributes.ToString()
                      .Should()
                      .Be("data-widget='{\"Id\":789,\"Name\":\"The Stanley\",\"Price\":24.99}'");
        }

        [Fact]
        public void Empty_id_attributes_are_not_rendered()
        {
            "div".Tag().WithAttributes(new HtmlAttributes { { "id", null } }).ToString()
                 .Should()
                 .NotContain("id");

            "div".Tag().WithAttributes(new HtmlAttributes { { "id", new HtmlString("") } }).ToString()
                 .Should()
                 .NotContain("id");

            "div".Tag().WithAttributes(new HtmlAttributes { { "id", string.Empty } }).ToString()
                 .Should()
                 .NotContain("id");
        }

        [Fact]
        public void Attributes_containing_JSON_values_are_not_reencoded()
        {
            const string expected = @"<option data-url='""/widgets/X-1""'>X-1 Widget</option>";

            var part =
                new
                {
                    Name = "X-1 Widget",
                    SerialNumber = "X-1"
                };

            var attr = new HtmlAttributes
            {
                { "data-url", ("/widgets/" + part.SerialNumber).JsonEncode() }
            };

            var opt = "option"
                      .Tag()
                      .Containing(part.Name)
                      .WithAttributes(attr);

            opt.Crunch()
               .Should()
               .Be(expected.Crunch());
        }

        [Fact]
        public void IsReadOnly_defaults_to_false()
        {
            new HtmlAttributes().IsReadOnly.Should().BeFalse();
        }

        [Fact]
        public void Can_be_cleared()
        {
            var attributes = new HtmlAttributes { { "one", 1 } };

            attributes.Count.Should().Be(1);
            attributes.Clear();
            attributes.Count.Should().Be(0);
        }
    }
}