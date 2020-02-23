// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Microsoft.DotNet.Interactive.Formatting
{
    /// <summary>
    /// Writes HTML using a C# DSL, bypassing the need for specialized parser and compiler infrastructure such as Razor.
    /// </summary>
    public class PocketView : DynamicObject, ITag
    {
        private readonly Dictionary<string, TagTransform> _transforms = new Dictionary<string, TagTransform>();
        private readonly Tag _tag;
        private TagTransform _transform;

        /// <summary>
        ///   Initializes a new instance of the <see cref="PocketView" /> class.
        /// </summary>
        /// <param name="nested"> A nested instance. </param>
        public PocketView(PocketView nested = null)
        {
            if (nested != null)
            {
                _transforms = nested._transforms;
            }
            else
            {
                AddDefaultTransforms();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PocketView"/> class.
        /// </summary>
        /// <param name="tagName">Name of the tag.</param>
        /// <param name="nested">A nested instance.</param>
        public PocketView(string tagName, PocketView nested = null) : this(nested)
        {
            _tag = tagName.Tag();
        }

        private void AddDefaultTransforms()
        {
            ((dynamic) this).br = Transform((t, u) => { t.SelfClosing(); });
            ((dynamic) this).input = Transform((t, u) => { t.SelfClosing(); });
        }

        /// <summary>
        /// Writes an element.
        /// </summary>
        public override bool TryGetMember(
            GetMemberBinder binder,
            out object result)
        {
            var returnValue = new PocketView(tagName: binder.Name, nested: this);

            if (_transforms.TryGetValue(binder.Name, out var transform))
            {
                returnValue._transform = transform;
            }

            result = returnValue;
            return true;
        }

        /// <summary>
        /// Writes an element.
        /// </summary>
        public override bool TryInvokeMember(
            InvokeMemberBinder binder,
            object[] args,
            out object result)
        {
            var pocketView = new PocketView(tagName: binder.Name, nested: this);

            pocketView.SetContent(args);

            if (_transforms.TryGetValue(binder.Name, out var transform))
            {
                var content = ComposeContent(binder.CallInfo.ArgumentNames, args);

                transform(pocketView._tag, content);
            }

            result = pocketView;
            return true;
        }

        /// <summary>
        ///   Writes tag content
        /// </summary>
        public override bool TryInvoke(
            InvokeBinder binder,
            object[] args,
            out object result)
        {
            SetContent(args);

            ApplyTransform(binder, args);

            result = this;
            return true;
        }

        private void ApplyTransform(
            InvokeBinder binder,
            object[] args)
        {
            if (_transform != null)
            {
                var content = ComposeContent(
                    binder?.CallInfo?.ArgumentNames,
                    args);

                _transform(_tag, content);

                // null out _transform so that it will only be applied once
                _transform = null;
            }
        }

        public override bool TrySetMember(
            SetMemberBinder binder,
            object value)
        {
            if (value is TagTransform alias)
            {
                _transforms.Add(binder.Name, alias);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Writes attributes.
        /// </summary>
        public override bool TryGetIndex(
            GetIndexBinder binder,
            object[] values,
            out object result)
        {
            var argumentNameIndex = 0;

            for (var i = 0; i < values.Length; i++)
            {
                var att = values[i];

                if (att is IDictionary<string, object> dict)
                {
                    HtmlAttributes.MergeWith(dict);
                }
                else
                {
                    var key = binder.CallInfo
                                    .ArgumentNames
                                    .ElementAt(argumentNameIndex++)
                                    .Replace("_", "-");
                    HtmlAttributes[key] = values[i];
                }
            }

            result = this;
            return true;
        }

        public void SetContent(object[] args)
        {
            if (args?.Length == 0)
            {
                return;
            }

            _tag.Content = writer => Write(args, writer);
        }

        private void Write(IReadOnlyList<object> args, TextWriter writer)
        {
            for (var i = 0; i < args.Count; i++)
            {
                var arg = args[i];

                switch (arg)
                {
                    case string s:
                        writer.Write(s.HtmlEncode());
                        break;

                    case IHtmlContent html:
                        html.WriteTo(writer, HtmlEncoder.Default);
                        break;

                    case IEnumerable<IHtmlContent> htmls:
                        Write(htmls.ToArray(), writer);
                        break;

                    default:
                        if (arg is IEnumerable<object> seq &&
                            seq.All(s => s is IHtmlContent))
                        {
                            Write(seq.OfType<IHtmlContent>().ToArray(), writer);
                        }
                        else
                        {
                            var e = arg
                                    .ToDisplayString()
                                    .HtmlEncode();

                            e.WriteTo(writer, HtmlEncoder.Default);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            if (_tag == null)
            {
                return "";
            }
            else
            {
                ApplyTransform(null, null);
                return _tag.ToString();
            }
        }

        /// <summary>
        ///   Gets HTML tag type.
        /// </summary>
        /// <value>The type of the tag.</value>
        public string Name
        {
            get
            {
                if (_tag == null)
                {
                    return "";
                }

                return _tag.Name;
            }
        }

        /// <summary>
        ///   Gets the HTML attributes to be rendered into the tag.
        /// </summary>
        /// <value>The HTML attributes.</value>
        public HtmlAttributes HtmlAttributes => _tag.HtmlAttributes;

        /// <summary>
        ///   Renders the tag to the specified <see cref = "TextWriter" />.
        /// </summary>
        /// <param name = "writer">The writer.</param>
        public void WriteTo(TextWriter writer, HtmlEncoder encoder)
        {
            _tag?.WriteTo(writer, encoder);
        }

        /// <summary>
        /// Creates a tag transform.
        /// </summary>
        /// <param name="transform">The transform.</param>
        /// <example>
        ///     _.textbox = Underscore.Transform(
        ///     (tag, model) =>
        ///     {
        ///        tag.TagName = "div";
        ///        tag.Content = w =>
        ///        {
        ///            w.Write(_.label[@for: model.name](model.name));
        ///            w.Write(_.input[value: model.value, type: "text", name: model.name]);
        ///        };
        ///     });
        /// 
        /// When called like this:
        /// 
        ///     _.textbox(name: "FirstName", value: "Bob")
        /// 
        /// This outputs: 
        /// 
        ///     <code>
        ///         <div>
        ///             <label for="FirstName">FirstName</label>
        ///             <input name="FirstName" type="text" value="Bob"></input>
        ///         </div>
        ///     </code>
        /// </example>
        public static object Transform(Action<Tag, dynamic> transform)
        {
            return new TagTransform(transform);
        }

        private delegate void TagTransform(Tag tag, object contents);

        private dynamic ComposeContent(
            IReadOnlyCollection<string> argumentNames,
            object[] args)
        {
            if (argumentNames?.Count == 0)
            {
                if (args?.Length > 0)
                {
                    return args;
                }

                return null;
            }

            var expando = new ExpandoObject();

            if (argumentNames != null)
            {
                expando
                    .MergeWith(
                        argumentNames.Zip(args, (name, value) => new { name, value })
                                     .ToDictionary(p => p.name, p => p.value));
            }

            return expando;
        }
    }
}