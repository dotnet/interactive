// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;

namespace Microsoft.DotNet.Interactive.Formatting
{
    /// <summary>
    ///   Represents an HTML tag.
    /// </summary>
    public class Tag : ITag
    {
        private HtmlAttributes _htmlAttributes;

        /// <summary>
        /// Initializes a new instance of the <see cref="Tag"/> class.
        /// </summary>
        /// <param name="name">The name of the tag.</param>
        public Tag(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tag"/> class.
        /// </summary>
        /// <param name="name">The name of the tag.</param>
        /// <param name="text">The text contained by the tag.</param>
        public Tag(string name, string text) : this(name)
        {
            Content = writer => writer.Write(text);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tag"/> class.
        /// </summary>
        /// <param name="name">Name of the tag.</param>
        /// <param name="content">The content.</param>
        public Tag(string name, Action<TextWriter> content) : this(name)
        {
            Content = content;
        }

        public Action<TextWriter> Content { get; set; }

        /// <summary>
        ///   Gets or sets a value indicating whether this instance is self closing.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is self closing; otherwise, <c>false</c>.
        /// </value>
        public bool IsSelfClosing { get; set; }

        /// <summary>
        ///   Gets HTML tag type.
        /// </summary>
        /// <value>
        ///   The type of the tag.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        ///   Gets the HTML attributes to be rendered into the tag.
        /// </summary>
        /// <value>
        ///   The HTML attributes.
        /// </value>
        public HtmlAttributes HtmlAttributes
        {
            get => _htmlAttributes ??= new HtmlAttributes();
            set => _htmlAttributes = value;
        }

        /// <summary>
        ///   Renders the tag to the specified <see cref = "TextWriter" />.
        /// </summary>
        /// <param name = "writer">The writer.</param>
        public virtual void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            if (Content == null && IsSelfClosing)
            {
                WriteSelfClosingTag(writer);
                return;
            }

            WriteStartTag(writer);
            WriteContentsTo(writer);
            WriteEndTag(writer);
        }

        protected void WriteSelfClosingTag(TextWriter writer)
        {
            writer.Write('<');
            writer.Write(Name);
            HtmlAttributes.WriteTo(writer, HtmlEncoder.Default);
            writer.Write(" />");
        }

        protected void WriteEndTag(TextWriter writer)
        {
            writer.Write("</");
            writer.Write(Name);
            writer.Write('>');
        }

        protected void WriteStartTag(TextWriter writer)
        {
            writer.Write('<');
            writer.Write(Name);
            HtmlAttributes.WriteTo(writer, HtmlEncoder.Default);
            writer.Write('>');
        }

        /// <summary>
        ///   Writes the tag contents (without outer HTML elements) to the specified writer.
        /// </summary>
        /// <param name = "writer">The writer.</param>
        protected virtual void WriteContentsTo(TextWriter writer)
        {
            Content?.Invoke(writer);
        }

        /// <summary>
        /// Merges the specified attributes into the tag's existing attributes.
        /// </summary>
        /// <param name="htmlAttributes">The HTML attributes to be merged.</param>
        /// <param name="replace">if set to <c>true</c> replace existing attributes when attributes with the same name have been previously defined; otherwise, ignore.</param>
        public void MergeAttributes(IDictionary<string, object> htmlAttributes, bool replace = false) =>
            HtmlAttributes.MergeWith(htmlAttributes, replace);

        /// <summary>
        ///   Returns a <see cref = "System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        ///   A <see cref = "System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var writer = new StringWriter();
            WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }
    }
}