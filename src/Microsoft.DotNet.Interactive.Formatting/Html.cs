// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using System.Web;
using Microsoft.AspNetCore.Html;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public static class Html
    {
        internal static IHtmlContent EnsureHtmlAttributeEncoded(this object source) =>
            source is null
                ? HtmlString.Empty
                : source as IHtmlContent ?? source.ToString().HtmlAttributeEncode();

        public static IHtmlContent HtmlEncode(this string content) =>
            new HtmlString(HttpUtility.HtmlEncode(content));

        public static IHtmlContent HtmlAttributeEncode(this string content) => new HtmlString(HttpUtility.HtmlAttributeEncode(content));

        public static IHtmlContent ToHtmlContent(this string value) =>
            new HtmlString(value);

        /// <summary>
        /// Designates that the tag, when rendered, will be self-closing.
        /// </summary>
        public static TTag SelfClosing<TTag>(this TTag tag)
            where TTag : HtmlTag
        {
            tag.IsSelfClosing = true;
            return tag;
        }

        /// <summary>
        /// Merges the specified attributes into the tag's existing attributes.
        /// </summary>
        public static TTag WithAttributes<TTag>(this TTag tag, IDictionary<string, object> htmlAttributes) where TTag : IHtmlTag
        {
            tag.HtmlAttributes.MergeWith(htmlAttributes, true);
            return tag;
        }

        public static TTag WithAttributes<TTag>(
            this TTag tag,
            string name,
            object value)
            where TTag : IHtmlTag
        {
            tag.HtmlAttributes.Add(name, value);

            return tag;
        }

        /// <summary>
        /// Creates a tag of the type specified by <paramref name="tagName" />.
        /// </summary>
        public static HtmlTag Tag(this string tagName)
        {
            return new HtmlTag(tagName);
        }

        /// <summary>
        /// Appends the specified tag to the source tag.
        /// </summary>
        /// <typeparam name="TTag">The type of the tag.</typeparam>
        /// <param name="toTag">The tag to which to append.</param>
        /// <param name="content">The tag to be appended.</param>
        /// <returns><paramref name="toTag" />.</returns>
        public static TTag Append<TTag>(this TTag toTag, IHtmlContent content) where TTag : HtmlTag
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
        public static TTag Append<TTag>(this TTag toTag, params IHtmlContent[] contents) where TTag : HtmlTag
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
        public static TTag AppendTo<TTag>(this TTag appendTag, HtmlTag toTag) where TTag : IHtmlTag
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
        public static TTag Containing<TTag>(this TTag tag, string text) where TTag : HtmlTag
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
        public static TTag Containing<TTag>(this TTag tag, IHtmlContent content) where TTag : HtmlTag
        {
            tag.Content = writer => writer.Write(content.ToString());
            return tag;
        }

        internal static TTag Containing<TTag>(this TTag tag, params IHtmlTag[] tags) where TTag : HtmlTag
        {
            return tag.Containing((IEnumerable<IHtmlTag>)tags);
        }

        internal static TTag Containing<TTag>(this TTag tag, IEnumerable<IHtmlTag> tags) where TTag : HtmlTag
        {
            tag.Content = writer =>
            {
                foreach (var childTag in tags)
                {
                    childTag.WriteTo(writer, HtmlEncoder.Default);
                }
            };
            return tag;
        }

        internal static TTag Containing<TTag>(this TTag tag, Action<TextWriter> content) where TTag : HtmlTag
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
        public static TTag Prepend<TTag>(this TTag toTag, IHtmlContent content) where TTag : HtmlTag
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
        public static TTag PrependTo<TTag>(this TTag prependTag, HtmlTag toTag) where TTag : IHtmlTag
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
        public static TTag WrapInner<TTag>(this TTag tag, HtmlTag wrappingTag) where TTag : HtmlTag
        {
            wrappingTag.Content = tag.Content;
            tag.Content = writer => wrappingTag.WriteTo(writer, HtmlEncoder.Default);
            return tag;
        }

        internal static PocketView Table(
            IReadOnlyList<IHtmlContent> headers,
            IReadOnlyList<IHtmlContent> rows) =>
            table(
                thead(
                    tr(
                        headers ?? Array.Empty<IHtmlContent>())),
                tbody(
                    rows));
    }
}