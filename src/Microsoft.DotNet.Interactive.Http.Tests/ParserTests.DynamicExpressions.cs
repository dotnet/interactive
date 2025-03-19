// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Http.Parsing;

namespace Microsoft.DotNet.Interactive.Http.Tests;

public partial class HttpParserTests
{
    [TestClass]
    public class DynamicExpressions
    {
        [TestMethod]
        public void can_bind_datetime_with_custom_format()
        {
            var expression = "$datetime 'dd-MM-yyyy' 1 d";
            var code = $@"@var = {{{{{expression}}}}}";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.UtcNow;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            binding.Value.As<string>().Should().Be(currentTime.AddDays(1.0).ToString("dd-MM-yyyy"));
        }

        [TestMethod]
        public void can_bind_datetime_uses_utcnow()
        {
            var expression = "$datetime 'Tzz'";
            var code = $@"@var = {{{{{expression}}}}}";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.UtcNow;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            // Since we want to make sure that the provided expression is evaluated
            // and .Now or .UtcNow is used as appropriate, we do not pass our own offset, like we do with other unit tests.
            // we can do this since we are passing a custom format which only includes the time zone.
            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, expression);
            binding.Value.As<string>().Should().Be(currentTime.ToString("Tzz"));
        }

        [TestMethod]
        public void can_bind_datetime_with_iso8601_format()
        {
            var expression = "$datetime iso8601";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.UtcNow.UtcDateTime;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            binding.Value.As<string>().Should().Be(currentTime.ToString("o"));
        }

        [TestMethod]
        public void can_bind_datetime_with_iso8601_format_with_offset()
        {
            var expression = "$datetime iso8601 1 y";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.UtcNow.UtcDateTime;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            binding.Value.As<string>().Should().Be(currentTime.AddYears(1).ToString("o"));
        }

        [TestMethod]
        public void can_bind_datetime_with_rfc1123_format()
        {
            var expression = "$datetime rfc1123";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.UtcNow;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            binding.Value.As<string>().Should().Be(currentTime.ToString("r"));
        }

        [TestMethod]
        public void can_bind_datetime_with_rfc1123_format_with_offset()
        {
            var expression = "$datetime rfc1123 1 d";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.UtcNow;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            binding.Value.As<string>().Should().Be(currentTime.AddDays(1).ToString("r"));
        }

        [TestMethod]
        public void can_bind_local_datetime_with_custom_format()
        {
            var expression = "$localDatetime 'dd-MM-yyyy' 1 d";
            var code = $@"@var = {{{{{expression}}}}}";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.Now;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            binding.Value.As<string>().Should().Be(currentTime.AddDays(1.0).ToString("dd-MM-yyyy"));
        }

        [TestMethod]
        public void can_bind_local_datetime_uses_now()
        {
            var expression = "$localDatetime 'Tzz'";
            var code = $@"@var = {{{{{expression}}}}}";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.Now;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            // Since we want to make sure that the provided expression is evaluated
            // and .Now or .UtcNow is used as appropriate, we do not pass our own offset, like we do with other unit tests.
            // we can do this since we are passing a custom format which only includes the time zone.
            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, expression);
            binding.Value.As<string>().Should().Be(currentTime.ToString("Tzz"));
        }

        [TestMethod]
        public void can_bind_local_datetime_with_iso8601_format()
        {
            var expression = "$localDatetime iso8601";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.UtcNow.UtcDateTime;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            binding.Value.As<string>().Should().Be(currentTime.ToString("o"));
        }

        [TestMethod]
        public void can_bind_local_datetime_with_iso8601_format_with_offset()
        {
            var expression = "$localDatetime iso8601 1 y";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.UtcNow.UtcDateTime;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            binding.Value.As<string>().Should().Be(currentTime.AddYears(1).ToString("o"));
        }

        [TestMethod]
        public void can_bind_local_datetime_with_rfc1123_format()
        {
            var expression = "$localDatetime rfc1123";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.Now;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            binding.Value.As<string>().Should().Be(currentTime.ToString("r"));
        }

        [TestMethod]
        public void can_bind_timestamp()
        {
            var expression = "$timestamp";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.Now;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            binding.Value.As<string>().Should().Be(currentTime.ToUnixTimeSeconds().ToString());
        }

        [TestMethod]
        public void can_bind_timestamp_with_offset()
        {
            var expression = "$timestamp 7 M";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.Now;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            binding.Value.As<string>().Should().Be(currentTime.AddMonths(7).ToUnixTimeSeconds().ToString());
        }

        [TestMethod]
        public void can_bind_randomInt_2_10()
        {
            using var _ = new AssertionScope();

            var expression = "$randomInt 2 10";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.Now;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            var stringValue = binding.Value.As<string>();
            Assert.IsTrue(int.TryParse(stringValue, out int value));
            value.Should().BeGreaterThanOrEqualTo(2).And.BeLessThan(10);
        }

        [TestMethod]
        public void can_bind_randomInt_3()
        {
            using var _ = new AssertionScope();

            var expression = "$randomInt 3";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.Now;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            var stringValue = binding.Value.As<string>();
            Assert.IsTrue(int.TryParse(stringValue, out int value));
            value.Should().BeGreaterThanOrEqualTo(0).And.BeLessThan(3);
        }

        [TestMethod]
        public void can_bind_randomInt()
        {
            using var _ = new AssertionScope();

            var expression = "$randomInt";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.Now;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            var stringValue = binding.Value.As<string>();
            Assert.IsTrue(int.TryParse(stringValue, out int value));
            value.Should().BeGreaterThanOrEqualTo(0);
        }

        [TestMethod]
        public void can_bind_guid()
        {
            using var _ = new AssertionScope();

            var expression = "$guid";
            var code = $@"@var = {{{{{expression}}}}}""";

            var result = HttpRequestParser.Parse(code);
            var currentTime = DateTimeOffset.Now;
            var node = result.SyntaxTree.RootNode.DescendantNodesAndTokens().OfType<HttpExpressionNode>().Single();

            var binding = DynamicExpressionUtilities.ResolveExpressionBinding(node, () => currentTime, expression);
            var stringValue = binding.Value.As<string>();
            Assert.IsTrue(Guid.TryParse(stringValue, out Guid value));
            value.Should().NotBeEmpty();
        }
    }
}
