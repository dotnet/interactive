// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.DotNet.Interactive.ValueSharing;
using static Microsoft.DotNet.Interactive.Formatting.PocketViewTags;

namespace Microsoft.DotNet.Interactive;

internal class KernelValuesFormatter : HtmlFormatter<KernelValues>
{
    public KernelValuesFormatter() : base(FormatKernelValues)
    {
    }

    private static bool FormatKernelValues(
        KernelValues instance, 
        FormatContext context)
    {
        PocketView output = null;

        context.RequireDefaultStyles();

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
                            td(div[@class: "dni-plaintext"](pre(v.Value.ToDisplayString())))
                        ))));
        }
        else
        {
            output = div(instance.Select(v => v.Name + "\t "));
        }

        output.WriteTo(context);

        return true;
    }
}