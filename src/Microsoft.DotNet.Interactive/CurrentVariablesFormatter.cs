// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using Microsoft.DotNet.Interactive.Formatting;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive
{
    public class CurrentVariablesFormatter : ITypeFormatter<CurrentVariables>
    {
        public string MimeType => "text/html";

        public Type Type => typeof(CurrentVariables);

        public bool Format(FormatContext context, CurrentVariables instance, TextWriter writer)
        {
            PocketView output = null;

        /// <summary>
        ///   Renders the tag to the specified <see cref = "TextWriter" />.
        /// </summary>
        /// <param name = "writer">The writer.</param>
            if (instance.Detailed)
            {
                var innerContext = context.ReduceContent(FormatContext.NestedInTable);
                output = table(
                    thead(
                        tr(
                            th("Variable"),
                            th("Type"),
                            th("Value"))),
                    tbody(
                        instance.Select(v =>
                             tr(
                                 // Note, embeds these as objects into the HTML content, ultimately rendered by PocketView 
                                 td(v.Name),
                                 td(embed(v.Type, innerContext)),
                                 td(embed(v.Value, innerContext))
                             ))));
            }
            else
            {
                output = div(instance.Select(v => v.Name + "\t "));
            }

            output.WriteTo(writer, HtmlEncoder.Default);
            return true;
        }

        public bool Format(FormatContext context, object instance, TextWriter writer)
        {
            if (instance is CurrentVariables variables)
            {
                return Format(context, variables, writer);
            }
            return false;
        }
    }
}
