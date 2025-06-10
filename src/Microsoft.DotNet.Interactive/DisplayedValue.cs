// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

public class DisplayedValue_OLD
{
    // FIX: (DisplayedValue) remove this type

    private KernelInvocationContext _context;

    public DisplayedValue_OLD(IReadOnlyList<FormattedValue> formattedValues, KernelInvocationContext context)
    {
        FormattedValues = formattedValues ?? throw new ArgumentNullException(nameof(formattedValues));
        _context = context ?? throw new ArgumentNullException(nameof(context));

        DisplayId = Guid.NewGuid().ToString();
    }

    public string DisplayId { get; }

    public IReadOnlyList<FormattedValue> FormattedValues { get; private set; }

    public void Update(object updatedValue)
    {
        var mimeTypes = FormattedValues.Select(x => x.MimeType).ToArray();

        if (_context.Command is NoCommand)
        {
            // If no context is available, we write to the console.
            var output = updatedValue.ToDisplayString(mimeTypes.FirstOrDefault() ?? "text/plain");
            Console.WriteLine(output);
            return;
        }

        FormattedValues = FormattedValue.CreateManyFromObject(updatedValue, mimeTypes);

        if (KernelInvocationContext.Current?.Command is SubmitCode)
        {
            _context = KernelInvocationContext.Current;
        }

        _context.Publish(new DisplayedValueUpdated(updatedValue, DisplayId, _context.Command, FormattedValues));
    }
}