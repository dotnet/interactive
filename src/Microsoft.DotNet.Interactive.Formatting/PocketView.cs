// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.Formatting;

/// <summary>
/// Writes HTML using a C# DSL, bypassing the need for specialized parser and compiler infrastructure such as Razor.
/// </summary>
public class PocketView : DynamicObject, IHtmlContent
{
    private readonly Dictionary<string, TagTransform> _transforms = new();
    private TagTransform _transform;
    private List<(string id, IHtmlContent content)> _dependentContent;

    /// <summary>
    ///   Initializes a new instance of the <see cref="PocketView" /> class.
    /// </summary>
    /// <param name="nested"> A nested instance. </param>
    public PocketView(PocketView nested = null)
    {
        if (nested is not null)
        {
            _transforms = nested._transforms;
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PocketView"/> class.
    /// </summary>
    /// <param name="tagName">Name of the tag.</param>
    /// <param name="nested">A nested instance.</param>
    public PocketView(string tagName, PocketView nested = null) : this(nested)
    {
        HtmlTag = tagName.Tag();
    }
        
    public HtmlTag HtmlTag { get; }

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

            transform(pocketView.HtmlTag, content, null);
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

        ApplyTransform(binder, args, null);

        result = this;
        return true;
    }

    private void ApplyTransform(
        InvokeBinder binder,
        object[] args, 
        FormatContext formatContext)
    {
        if (_transform is not null)
        {
            var content = ComposeContent(
                binder?.CallInfo?.ArgumentNames,
                args);

            _transform(HtmlTag, content, formatContext);

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
            _transforms[binder.Name] = alias;

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
        var offset = values.Length - binder.CallInfo.ArgumentNames.Count;

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];

            if (value is IDictionary<string, object> dict)
            {
                HtmlAttributes.MergeWith(dict);
            }
            else
            {
                if (i >= offset)
                {
                    var key = binder.CallInfo
                                    .ArgumentNames
                                    .ElementAt(i - offset)
                                    .Replace("_", "-");

                    HtmlAttributes[key] = value;
                }
                else if (value is string s)
                {
                    HtmlAttributes[s] = null;
                }
            }
        }

        result = this;
        return true;
    }

    public void AddDependency(string id, IHtmlContent content)
    {
        if (_dependentContent is null)
        {
            _dependentContent = new();
        }

        _dependentContent.Add((id, content));
    }

    public virtual void SetContent(object[] args)
    {
        if (args?.Length == 0)
        {
            return;
        }

        HtmlTag.Content = HtmlTagContent;

        void HtmlTagContent(FormatContext context) => 
            Write(args, context);
    }

    private void Write(
        IReadOnlyList<object> args, 
        FormatContext context)
    {
        for (var i = 0; i < args.Count; i++)
        {
            var arg = args[i];

            switch (arg)
            {
                case string s:
                    context.Writer.Write(s.HtmlEncode());
                    break;

                case PocketView view:
                    view.WriteTo(context);
                    break;

                case HtmlTag tag:
                    tag.WriteTo(context);
                    break;

                case IHtmlContent html:
                    html.WriteTo(context.Writer, HtmlEncoder.Default);
                    break;

                case IEnumerable<IHtmlContent> htmls:
                    Write(htmls.ToArray(), context);
                    break;
                        
                default:
                    if (arg is IEnumerable<object> seq &&
                        seq.All(s => s is IHtmlContent))
                    {
                        Write(seq.OfType<IHtmlContent>().ToArray(), context);
                    }
                    else
                    {
                        arg.FormatTo(context, HtmlFormatter.MimeType);
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
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        using (var formatContext = new FormatContext(writer))
        {
            ApplyTransform(null, null, formatContext);
            HtmlTag.WriteTo(formatContext);
        }

        return writer.ToString();
    }

    /// <summary>
    ///   Gets the HTML attributes to be rendered into the tag.
    /// </summary>
    /// <value>The HTML attributes.</value>
    public HtmlAttributes HtmlAttributes => HtmlTag.HtmlAttributes;

    /// <summary>
    ///   Renders the tag to the specified <see cref = "TextWriter" />.
    /// </summary>
    /// <param name = "writer">The writer.</param>
    /// <param name="encoder">An HTML encoder.</param>
    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        HtmlTag.WriteTo(writer, encoder);
    }
        
    public void WriteTo(FormatContext context)
    {
        HtmlTag.WriteTo(context);

        if (_dependentContent is not null)
        {
            for (var i = 0; i < _dependentContent.Count; i++)
            {
                var item = _dependentContent[i];
                context.RequireOnComplete(item.id, item.content);
            }

            _dependentContent = null;
        }
    }

    /// <summary>
    /// Creates a tag transform.
    /// </summary>
    /// <param name="transform">The transform.</param>
    /// <example>
    ///     _.textbox = PocketView.Transform(
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
    public static object Transform(Action<HtmlTag, dynamic> transform)
    {
        void TagTransform(HtmlTag tag, object contents, FormatContext _) => transform(tag, contents);

        return new TagTransform(TagTransform);
    }

    private delegate void TagTransform(HtmlTag tag, object contents, FormatContext context = null);

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

        if (argumentNames is not null)
        {
            expando
                .MergeWith(
                    argumentNames.Zip(args, (name, value) => new { name, value })
                        .ToDictionary(p => p.name, p => p.value));
        }

        return expando;
    }
}