// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive;

public class DisplayedValue
{
    private readonly string _displayId;
    private readonly HashSet<string> _mimeTypes;
    private KernelInvocationContext _context;

    public DisplayedValue(string displayId, string mimeType, KernelInvocationContext context) : this(displayId, new[] { mimeType }, context)
    {
    }

    public DisplayedValue(string displayId, string[] mimeTypes, KernelInvocationContext context)
    {
        if (string.IsNullOrWhiteSpace(displayId))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(displayId));
        }

        if (mimeTypes is null || mimeTypes.Count(mimetype => !string.IsNullOrWhiteSpace(mimetype)) == 0)
        {
            throw new ArgumentException("Value cannot be null or empty.", nameof(mimeTypes));
        }

        _displayId = displayId;
        _mimeTypes = new HashSet<string>(mimeTypes.Where(mimetype => !string.IsNullOrWhiteSpace(mimetype)));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IReadOnlyCollection<string> MimeTypes => _mimeTypes;

    public void Update(object updatedValue)
    {
        var formattedValues = MimeTypes.Select(mimeType => new FormattedValue(
            mimeType,
            updatedValue.ToDisplayString(mimeType))).ToArray();

        if (KernelInvocationContext.Current?.Command is SubmitCode)
        {
            _context = KernelInvocationContext.Current;
        }

        _context.Publish(new DisplayedValueUpdated(updatedValue, _displayId, _context.Command, formattedValues));
    }
}