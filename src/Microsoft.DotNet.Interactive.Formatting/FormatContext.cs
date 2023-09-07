// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Pocket;

namespace Microsoft.DotNet.Interactive.Formatting;

public class FormatContext : IDisposable
{
    private Dictionary<string, Action<FormatContext>> _requiredContent;
    private bool _disableRecursion;

    public FormatContext(TextWriter writer)
    {
        Writer = writer;
    }

    public int Depth { get; private set; }

    internal int TableDepth { get; private set; }

    public TextWriter Writer { get; }

    internal void RequireOnComplete(string id, IHtmlContent content)
    {
        if (_requiredContent is null)
        {
            _requiredContent = new();
        }

        if (!_requiredContent.ContainsKey(id))
        {
            _requiredContent.Add(id, WriteContent);
        }

        void WriteContent(FormatContext context) => content.WriteTo(context.Writer, HtmlEncoder.Default);
    }

    internal IDisposable IncrementDepth()
    {
        Depth++;
        return Disposable.Create(() => Depth--);
    }

    internal IDisposable IncrementTableDepth()
    {
        TableDepth++;
        return Disposable.Create(() => TableDepth--);
    }

    internal bool AllowRecursion =>
        Depth <= Formatter.RecursionLimit &&
        !_disableRecursion;

    internal bool DisableRecursion() => _disableRecursion = true;

    internal bool EnableRecursion() => _disableRecursion = false;

    internal bool IsStartingObjectWithinSequence { get; set; }

    public void Dispose()
    {
        if (_requiredContent is not null)
        {
            foreach (var require in _requiredContent.Values)
            {
                require(this);
            }

            _requiredContent = null;
        }
    }
}