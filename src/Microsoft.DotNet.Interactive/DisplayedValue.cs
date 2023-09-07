// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;

namespace Microsoft.DotNet.Interactive;

public class DisplayedValue
{
    private KernelInvocationContext _context;

    public DisplayedValue(IReadOnlyList<FormattedValue> formattedValues, KernelInvocationContext context)
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

        FormattedValues = FormattedValue.CreateManyFromObject(updatedValue, mimeTypes);

        if (KernelInvocationContext.Current?.Command is SubmitCode)
        {
            _context = KernelInvocationContext.Current;
        }

        _context.Publish(new DisplayedValueUpdated(updatedValue, DisplayId, _context.Command, FormattedValues));
    }
}