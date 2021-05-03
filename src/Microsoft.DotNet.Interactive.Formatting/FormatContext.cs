// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Pocket;

namespace Microsoft.DotNet.Interactive.Formatting
{
    public class FormatContext : IDisposable
    {
        private HashSet<IHtmlContent> _dependentContent;

        public FormatContext() : this(new StringWriter())
        {
        }

        public FormatContext(TextWriter writer)
        {
            Writer = writer;
        }

        internal int Depth { get; private set; }

        internal int TableDepth { get; private set; }

        public TextWriter Writer { get; }

        internal void AddDependentContent(IHtmlContent content) => (_dependentContent ??= new()).Add(content);

        /// <summary>Indicates a typical setting to reduce content in inner positions of a table.</summary>
        /// <remarks>When this reduction is applied, further nested tables and property expansions are avoided.</remarks>
        public const double NestedInTable = 0.2;

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

        public void Dispose()
        {
            if (_dependentContent is not null)
            {
                foreach (var content in _dependentContent)
                {
                    content.WriteTo(Writer, HtmlEncoder.Default);
                }

                _dependentContent = null;
            }
        }
    }
}