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

        public void Format(CurrentVariables instance, TextWriter writer)
        {
            PocketView output = null;

            if (instance.Detailed)
            {
                output = table(
                    thead(
                        tr(
                            th("Variable"),
                            th("Type"),
                            th("Value"))),
                    tbody(
                        instance.Select(v =>
                             tr(
                                 td(v.Name),
                                 td(v.Type),
                                 td(v.Value.ToDisplayString())
                             ))));
            }
            else
            {
                output = div(instance.Select(v => v.Name + "\t "));
            }

            output.WriteTo(writer, HtmlEncoder.Default);
        }

        public void Format(object instance, TextWriter writer)
        {
            if (instance is CurrentVariables variables)
            {
                Format(variables, writer);
            }
        }
    }
}
