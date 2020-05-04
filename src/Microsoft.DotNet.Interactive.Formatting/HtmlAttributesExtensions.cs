// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Formatting
{
    /// <summary>
    ///   Provides additional functionality for rendering HTML attributes.
    /// </summary>
    public static class HtmlAttributesExtensions
    {
        /// <summary>
        ///   Sets or removes the "checked" attribute.
        /// </summary>
        /// <param name = "attributes">The attributes to modify.</param>
        /// <param name = "value">Specifies whether to set the "checked" attribute. If set to false, this will remove the "checked" attribute.</param>
        /// <returns>
        ///   The modified <see cref = "HtmlAttributes" /> instance.
        /// </returns>
        public static HtmlAttributes Checked(
            this HtmlAttributes attributes,
            bool value = true)
        {
            if (value)
            {
                attributes["checked"] = "checked";
            }
            else
            {
                attributes.Remove("checked");
            }

            return attributes;
        }

        /// <summary>
        ///   Adds the specified class or classes to the attributes.
        /// </summary>
        /// <param name = "attributes">The attributes to which to add classes.</param>
        /// <param name = "classes">The class or classes to be added.</param>
        /// <returns>The modified <see cref = "HtmlAttributes" /> instance.</returns>
        /// <remarks>
        ///   The classes are merged with the existing classes on the <see cref = "HtmlAttributes" /> instance.
        /// </remarks>
        public static HtmlAttributes Class(
            this HtmlAttributes attributes,
            string classes)
        {
            attributes.AddCssClass(classes);
            return attributes;
        }

        /// <summary>
        /// Adds the specified class or classes to the attributes.
        /// </summary>
        /// <param name="attributes">The attributes to which to add classes.</param>
        /// <param name="classes">The class or classes to be added.</param>
        /// <param name="include">If set to <c>true</c>, add the class; otherwise, remove it.</param>
        /// <returns>
        /// The modified <see cref="HtmlAttributes"/> instance.
        /// </returns>
        /// <remarks>
        /// The classes are merged with the existing classes on the <see cref="HtmlAttributes"/> instance.
        /// </remarks>
        public static HtmlAttributes Class(
            this HtmlAttributes attributes,
            string classes,
            bool include)
        {
            if (include)
            {
                attributes.AddCssClass(classes);
            }
            else
            {
                attributes.RemoveCssClass(classes);
            }

            return attributes;
        }

        public static HtmlAttributes Data(this HtmlAttributes attributes, string key, object value)
        {
            var actualKey = key.StartsWith("data-") ? key : "data-" + key;
            attributes.Add(actualKey, value.SerializeToJson());
            return attributes;
        }

        /// <summary>
        ///   Adds a data-* attribute to the tag with the value serialized to JSON.
        /// </summary>
        /// <param name = "attributes">The attributes to which to add the data.</param>
        /// <param name = "key">The name of the attribute.</param>
        /// <param name = "value">The value to be serialized to JSON and inserted into the attribute's value.</param>
        /// <returns>The modified <see cref = "HtmlAttributes" /> instance.</returns>
        public static HtmlAttributes Data(
            this HtmlAttributes attributes,
            string key,
            string value)
        {
            var actualKey = key.StartsWith("data-") ? key : "data-" + key;
            attributes.Add(actualKey, value);
            return attributes;
        }

        /// <summary>
        ///   Adds a data-* attribute to the tag with the value serialized to JSON.
        /// </summary>
        /// <param name = "attributes">The attributes to which to add the data.</param>
        /// <param name = "key">The name of the attribute.</param>
        /// <param name = "value">The value to be serialized to JSON and inserted into the attribute's value.</param>
        /// <returns>The modified <see cref = "HtmlAttributes" /> instance.</returns>
        public static HtmlAttributes Data(
            this HtmlAttributes attributes,
            string key,
            IHtmlContent value)
        {
            var actualKey = key.StartsWith("data-") ? key : "data-" + key;
            attributes.Add(actualKey, value);
            return attributes;
        }

        /// <summary>
        ///   Sets or removes the "disabled" attribute.
        /// </summary>
        /// <param name = "attributes">The attributes to modify.</param>
        /// <param name = "value">Specifies whether to set the "disabled" attribute. If set to false, this will remove the "disabled" attribute.</param>
        /// <returns>
        ///   The modified <see cref = "HtmlAttributes" /> instance.
        /// </returns>
        public static HtmlAttributes Disabled(
            this HtmlAttributes attributes,
            bool value = true)
        {
            if (value)
            {
                attributes["disabled"] = "disabled";
            }
            else
            {
                attributes.Remove("disabled");
            }

            return attributes;
        }

        /// <summary>
        ///   Sets or removes the "selected" attribute.
        /// </summary>
        /// <param name = "attributes">The attributes to modify.</param>
        /// <param name = "value">Specifies whether to set the "selected" attribute. If set to false, this will remove the "selected" attribute.</param>
        /// <returns>
        ///   The modified <see cref = "HtmlAttributes" /> instance.
        /// </returns>
        public static HtmlAttributes Selected(
            this HtmlAttributes attributes,
            bool value = true)
        {
            if (value)
            {
                attributes["selected"] = "selected";
            }
            else
            {
                attributes.Remove("selected");
            }

            return attributes;
        }

        /// <summary>
        ///   Determines whether attributes contain the specified class.
        /// </summary>
        /// <param name = "attributes">The attributes.</param>
        /// <param name = "class">The class to check for.</param>
        /// <returns>
        ///   <c>true</c> if the specified attributes has the specified class; otherwise, <c>false</c>.
        /// </returns>
        public static bool HasClass(
            this HtmlAttributes attributes,
            string @class)
        {
            return (attributes["class"] ?? string.Empty)
                   .ToString()
                   .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                   .Any(c => c == @class);
        }

        /// <summary>
        /// Adds the specified attribute.
        /// </summary>
        /// <param name="htmlAttributes">The HTML attributes to which to add an attribute.</param>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        internal static HtmlAttributes Attr(
            this HtmlAttributes htmlAttributes,
            string name,
            object value)
        {
            htmlAttributes[name] = value;
            return htmlAttributes;
        }
    }
}