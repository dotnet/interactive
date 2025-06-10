// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
        OnFormatterEvent?.Invoke(displayedValue);
    }
}