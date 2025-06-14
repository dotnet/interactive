// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Formatting;

public class DisplayedValue
{
    private string _displayId;

    public DisplayedValue(object value, IReadOnlyList<FormattedValue> formattedValues)
    {
        Value = value;

        FormattedValues = formattedValues ?? throw new ArgumentNullException(nameof(formattedValues));
    }

    public string DisplayId => _displayId ??= Guid.NewGuid().ToString();

    public bool IsUpdated { get; private set; }

    public IReadOnlyList<FormattedValue> FormattedValues { get; private set; }

    public object Value { get; private set; }

    public void Update(object updatedValue)
    {
        IsUpdated = true;

        Value = updatedValue;

        var mimeTypes = FormattedValues.Select(x => x.MimeType).ToArray();

        FormattedValues = FormattedValue.CreateManyFromObject(updatedValue, mimeTypes);

        Formatter.RaiseFormatterEvent(this);
    }
}