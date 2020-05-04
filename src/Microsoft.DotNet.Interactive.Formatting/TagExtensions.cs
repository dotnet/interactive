// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Formatting
{
    /// <summary>
    /// Provides functionality for writing HTML tags.
    /// </summary>
    public static class TagExtensions
    {
        /// <summary>
        /// Designates that the tag, when rendered, will be self-closing.
        /// </summary>
        public static TTag SelfClosing<TTag>(this TTag tag)
            where TTag : Tag
        {
            tag.IsSelfClosing = true;
            return tag;
        }

        /// <summary>
        /// Merges the specified attributes into the tag's existing attributes.
        /// </summary>
        public static TTag WithAttributes<TTag>(this TTag tag, IDictionary<string, object> htmlAttributes) where TTag : ITag
        {
            tag.HtmlAttributes.MergeWith(htmlAttributes, true);
            return tag;
        }

        public static TTag WithAttributes<TTag>(
            this TTag tag,
            string name,
            object value)
            where TTag : ITag
        {
            tag.HtmlAttributes.Add(name, value);

            return tag;
        }

        /// <summary>
        /// Creates a tag of the type specified by <paramref name="tagName" />.
        /// </summary>
        public static Tag Tag(this string tagName)
        {
            return new Tag(tagName);
        }

        /// <summary>
        /// Appends the specified tag to the source tag.
        /// </summary>
        /// <typeparam name="TTag">The type of the tag.</typeparam>
        /// <param name="toTag">The tag to which to append.</param>
        /// <param name="content">The tag to be appended.</param>
        /// <returns><paramref name="toTag" />.</returns>
        public static TTag Append<TTag>(this TTag toTag, IHtmlContent content) where TTag : Tag
        {
            Action<TextWriter> writeOriginalContent = toTag.Content;
            toTag.Content = writer =>
            {
                writeOriginalContent?.Invoke(writer);
                writer.Write(content);
            };
            return toTag;
        }

        /// <summary>
        ///   Appends the specified tags to the source tag.
        /// </summary>
        /// <typeparam name="TTag"> The type of <paramref name="toTag" /> . </typeparam>
        /// <param name="toTag"> To tag to which other tags will be appended. </param>
        /// <param name="contents"> The tags to append. </param>
        /// <returns> <paramref name="toTag" /> . </returns>
        public static TTag Append<TTag>(this TTag toTag, params IHtmlContent[] contents) where TTag : Tag
        {
            Action<TextWriter> writeOriginalContent = toTag.Content;
            toTag.Content = writer =>
            {
                writeOriginalContent?.Invoke(writer);

                for (int i = 0; i < contents.Length; i++)
                {
                    writer.Write(contents[i]);
                }
            };
            return toTag;
        }

        /// <summary>
        /// Appends a tag to the source tag.
        /// </summary>
        /// <typeparam name="TTag">The type of the source tag.</typeparam>
        /// <param name="appendTag">The tag to be appended.</param>
        /// <param name="toTag">The tag to which to append <paramref name="appendTag" />.</param>
        /// <returns><paramref name="appendTag" />.</returns>
        public static TTag AppendTo<TTag>(this TTag appendTag, Tag toTag) where TTag : ITag
        {
            toTag.Append(appendTag);
            return appendTag;
        }

        /// <summary>
        /// Specifies the contents of a tag.
        /// </summary>
        /// <typeparam name="TTag">The type of the tag.</typeparam>
        /// <param name="tag">The tag name.</param>
        /// <param name="text">The text which the tag should contain.</param>
        /// <returns>The same tag instance, with the contents set to the specified text.</returns>
        public static TTag Containing<TTag>(this TTag tag, string text) where TTag : Tag
        {
            return tag.Containing(text.HtmlEncode());
        }

        /// <summary>
        /// Specifies the contents of a tag.
        /// </summary>
        /// <typeparam name="TTag">The type of the tag.</typeparam>
        /// <param name="tag">The tag name.</param>
        /// <param name="content">The content of the tag.</param>
        /// <returns>The same tag instance, with the contents set to the specified text.</returns>
        public static TTag Containing<TTag>(this TTag tag, IHtmlContent content) where TTag : Tag
        {
            tag.Content = w => w.Write(content.ToString());
            return tag;
        }

        internal static TTag Containing<TTag>(this TTag tag, params ITag[] tags) where TTag : Tag
        {
            return tag.Containing((IEnumerable<ITag>) tags);
        }

        internal static TTag Containing<TTag>(this TTag tag, IEnumerable<ITag> tags) where TTag : Tag
        {
            tag.Content = w =>
            {
                foreach (var childTag in tags)
                {
                    childTag.WriteTo(w, HtmlEncoder.Default);
                }
            };
            return tag;
        }

        internal static TTag Containing<TTag>(this TTag tag, Action<TextWriter> content) where TTag : Tag
        {
            tag.Content = content;
            return tag;
        }

        /// <summary>
        ///   Prepends the specified tags to the source tag.
        /// </summary>
        /// <typeparam name="TTag"> The type of <paramref name="toTag" /> . </typeparam>
        /// <param name="toTag"> To tag to which other tags will be prepended. </param>
        /// <param name="content"> The tags to prepend. </param>
        /// <returns> <paramref name="toTag" /> . </returns>
        public static TTag Prepend<TTag>(this TTag toTag, IHtmlContent content) where TTag : Tag
        {
            Action<TextWriter> writeOriginalContent = toTag.Content;
            toTag.Content = writer =>
            {
                writer.Write(content);
                writeOriginalContent?.Invoke(writer);
            };
            return toTag;
        }

        /// <summary>
        /// Prepends a tag to the source tag.
        /// </summary>
        /// <typeparam name="TTag">The type of the source tag.</typeparam>
        /// <param name="prependTag">The tag to be prepended.</param>
        /// <param name="toTag">The tag to which to prepend <paramref name="prependTag" />.</param>
        /// <returns><paramref name="prependTag" />.</returns>
        public static TTag PrependTo<TTag>(this TTag prependTag, Tag toTag) where TTag : ITag
        {
            toTag.Prepend(prependTag);
            return prependTag;
        }

        /// <summary>
        /// Wraps a tag's content in the specified tag.
        /// </summary>
        /// <typeparam name="TTag">The type of the tag.</typeparam>
        /// <param name="tag">The tag.</param>
        /// <param name="wrappingTag">The wrapping tag.</param>
        /// <returns></returns>
        public static TTag WrapInner<TTag>(this TTag tag, Tag wrappingTag) where TTag : Tag
        {
            wrappingTag.Content = tag.Content;
            tag.Content = writer => wrappingTag.WriteTo(writer, HtmlEncoder.Default);
            return tag;
        }
    }
}