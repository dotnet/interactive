// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using System.Text.Json;

namespace Microsoft.DotNet.Interactive.Formatting.Tests;

public sealed partial class FormatterTests : FormatterTestBase
{
    [TestClass]
    public class MimeTypeSettings : FormatterTestBase
    {
        [TestMethod]
        [DataRow("text/plain")]
        [DataRow("text/html")]
        public void Default_mime_type_sets_mime_type_for_all_types(string mimeType)
        {
            Formatter.DefaultMimeType = mimeType;

            Formatter.GetPreferredMimeTypesFor(typeof(int)).Should().BeEquivalentTo(mimeType);
            Formatter.GetPreferredMimeTypesFor(typeof(object)).Should().BeEquivalentTo(mimeType);
            Formatter.GetPreferredMimeTypesFor(typeof(Type)).Should().BeEquivalentTo(mimeType);
        }

        [TestMethod]
        [DataRow("text/plain")]
        [DataRow("text/html")]
        public void Type_specific_mime_type_preference_applies_to_derived_types(string mimeType)
        {
            Formatter.SetPreferredMimeTypesFor(typeof(object), mimeType);

            Formatter.GetPreferredMimeTypesFor(typeof(int)).Should().BeEquivalentTo(mimeType);
            Formatter.GetPreferredMimeTypesFor(typeof(object)).Should().BeEquivalentTo(mimeType);
            Formatter.GetPreferredMimeTypesFor(typeof(string)).Should().BeEquivalentTo(mimeType);
            Formatter.GetPreferredMimeTypesFor(typeof(Type)).Should().BeEquivalentTo(mimeType);
            Formatter.GetPreferredMimeTypesFor(typeof(JsonElement)).Should().BeEquivalentTo(mimeType);
        }

        [TestMethod]
        [DataRow("text/plain")]
        [DataRow("text/html")]
        [DataRow("text/whacky")]
        public void Last_specified_type_specific_mime_type_preference_applies_to_non_specified_derived_types(string mimeType)
        {
            // the last one should win
            Formatter.SetPreferredMimeTypesFor(typeof(object), mimeType);
            Formatter.SetPreferredMimeTypesFor(typeof(object), "text/plain");
            Formatter.SetPreferredMimeTypesFor(typeof(object), mimeType);
            Formatter.SetPreferredMimeTypesFor(typeof(object), "text/html");
            Formatter.SetPreferredMimeTypesFor(typeof(object), mimeType);

            Formatter.GetPreferredMimeTypesFor(typeof(int)).Should().BeEquivalentTo(mimeType);
            Formatter.GetPreferredMimeTypesFor(typeof(object)).Should().BeEquivalentTo(mimeType);
            Formatter.GetPreferredMimeTypesFor(typeof(string)).Should().BeEquivalentTo(mimeType);
            Formatter.GetPreferredMimeTypesFor(typeof(Type)).Should().BeEquivalentTo(mimeType);
            Formatter.GetPreferredMimeTypesFor(typeof(JsonElement)).Should().BeEquivalentTo(mimeType);
        }

        [TestMethod]
        [DataRow("text/plain")]
        [DataRow("text/html")]
        [DataRow("text/whacky")]
        public void Formatters_can_override_default_preference_for_base_type(string mimeType)
        {
            Formatter.SetPreferredMimeTypesFor(typeof(object), "text/default");
            Formatter.SetPreferredMimeTypesFor(typeof(int), mimeType);

            Formatter.GetPreferredMimeTypesFor(typeof(int)).Should().BeEquivalentTo(mimeType);
        }

        [TestMethod]
        [DataRow("text/plain")]
        [DataRow("text/html")]
        [DataRow("text/whacky")]
        public void Formatters_can_override_default_mime_type(string mimeType)
        {
            Formatter.DefaultMimeType = "text/default";
            Formatter.SetPreferredMimeTypesFor(typeof(int), mimeType);

            Formatter.GetPreferredMimeTypesFor(typeof(int)).Should().BeEquivalentTo(mimeType);
        }

        [TestMethod]
        [DataRow("text/plain")]
        [DataRow("text/html")]
        [DataRow("text/whacky")]
        public void Formatters_can_clear_default_preference_for_single_type(string mimeType)
        {
            Formatter.SetPreferredMimeTypesFor(typeof(int), mimeType);
            Formatter.ResetToDefault();

            Formatter.GetPreferredMimeTypesFor(typeof(int)).Should().BeEquivalentTo("text/html");
        }
    }
}