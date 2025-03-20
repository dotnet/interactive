// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

[TestClass]
public class HtmlAttributesTests : FormatterTestBase
{
    [TestMethod]
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

    [TestMethod]
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

    [TestMethod]
    public virtual void Dictionary_Add_adds_items_to_dictionary()
    {
        var attributes = new HtmlAttributes();

        attributes.Add("foo", "bar");

        var value = attributes["foo"].ToString();

        value.Should().Be("bar");
    }

    [TestMethod]
    public virtual void Collection_initializer_adds_items_to_dictionary()
    {
        var attributes = new HtmlAttributes { { "foo", "bar" } };

        var value = attributes["foo"].ToString();

        value.Should().Be("bar");
    }

    [TestMethod]
    public virtual void When_HtmlAttributes_is_empty_ToString_returns_empty_string()
    {
        var attributes = new HtmlAttributes();

        var value = attributes.ToString();

        value.Should().BeEmpty();
    }

    [TestMethod]
    public virtual void ToString_returns_correct_value_for_single_dictionary_entry()
    {
        var attributes = new HtmlAttributes { { "foo", "bar" } };

        attributes.ToString().Should().Be("foo=\"bar\"");
    }

    [TestMethod]
    public virtual void ToString_returns_correct_value_for_multiple_dictionary_entries()
    {
        var attributes = new HtmlAttributes
        {
            { "one", "1" },
            { "two", "2" },
        };

        attributes.ToString().Should().Be("one=\"1\" two=\"2\"");
    }

    [TestMethod]
    public virtual void Dynamic_assignment_adds_items_to_dictionary()
    {
        dynamic attributes = new HtmlAttributes();

        attributes.foo = "bar";

        string value = attributes["foo"];

        value.Should().Be("bar");
    }

    [TestMethod]
    public virtual void Dictionary_items_can_be_dynamically_retrieved()
    {
        dynamic attributes = new HtmlAttributes { { "foo", "bar" } };

        string foo = attributes.foo;

        foo.Should().Be("bar");
    }

    [TestMethod]
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

    [TestMethod]
    public virtual void Classes_are_aggregated_by_AddCssClass()
    {
        var attributes = new HtmlAttributes();

        attributes.AddCssClass("bar");
        attributes.AddCssClass("foo");

        attributes["class"].Should().Be("foo bar");
    }

    [TestMethod]
    public virtual void When_multiple_Add_calls_use_same_key_it_throws()
    {
        var attributes = new HtmlAttributes();

        attributes.Add("class", "one");

        Assert.Throws<ArgumentException>(() => attributes.Add("class", "two"));
    }

    [TestMethod]
    public virtual void Classes_are_overwritten_during_set_operations()
    {
        var attributes = new HtmlAttributes();

        attributes.Add("class", "one");
        attributes["class"] = "two";

        attributes["class"].Should().Be("two");
    }

    [TestMethod]
    public virtual void Classes_are_overwritten_during_dynamic_set_operations()
    {
        dynamic attributes = new HtmlAttributes();

        attributes.Add("class", "one");
        attributes.@class = "two";

        string @class = attributes.@class;

        @class.Should().Be("two");
    }

    [TestMethod]
    public virtual void Can_remove_dynamically_assigned_value()
    {
        var attributes = new HtmlAttributes();

        ((dynamic) attributes).foo = "bar";

        attributes.Remove("foo");

        attributes.Count.Should().Be(0);
    }

    [TestMethod]
    public virtual void ContainsKey_is_true_for_dynamically_assigned_property()
    {
        var attributes = new HtmlAttributes();

        ((dynamic) attributes).foo = "bar";

        attributes.ContainsKey("foo").Should().BeTrue();
    }

    [TestMethod]
    public virtual void Can_convert_arbitrary_named_parameters_into_attributes()
    {
        dynamic attributes = new HtmlAttributes();

        attributes.MergeWith(@class: "required", style: "display:block");

        string output = attributes.ToString();

        output.Should()
            .Contain("class=\"required\" style=\"display:block\"");
    }

    [TestMethod]
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

    [TestMethod]
    public void IsReadOnly_defaults_to_false()
    {
        new HtmlAttributes().IsReadOnly.Should().BeFalse();
    }

    [TestMethod]
    public void Can_be_cleared()
    {
        var attributes = new HtmlAttributes { { "one", 1 } };

        attributes.Count.Should().Be(1);
        attributes.Clear();
        attributes.Count.Should().Be(0);
    }
}