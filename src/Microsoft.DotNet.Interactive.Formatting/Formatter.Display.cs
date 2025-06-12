// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Pocket;

namespace Microsoft.DotNet.Interactive.Formatting;

public static partial class Formatter
{
    private static event Action<DisplayedValue> OnFormatterEvent;

    public static IDisposable SubscribeToDisplayedValues(Action<DisplayedValue> onEvent)
    {
        OnFormatterEvent += onEvent;

        return Disposable.Create(() => OnFormatterEvent -= onEvent);
    }

    internal static void RaiseFormatterEvent(DisplayedValue displayedValue)
    {
        if (OnFormatterEvent is { } handler)
        {
            handler(displayedValue);
        }
        else
        {
            WriteToConsole(displayedValue);
        }
    }

    public static void WriteToConsole(DisplayedValue displayedValue)
    {
        var formattedValue = displayedValue.FormattedValues.FirstOrDefault(v => v.MimeType == PlainTextFormatter.MimeType) ??
                             displayedValue.FormattedValues.FirstOrDefault();

        if (formattedValue?.Value is { } value)
        {
            Console.WriteLine(value);
        }
    }
}