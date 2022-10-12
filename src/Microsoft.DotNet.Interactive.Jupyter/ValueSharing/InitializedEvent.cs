// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Jupyter.ValueSharing;

[ValueAdapterEvent(ValueAdapterEventTypes.Initialized)]
public class InitializedEvent : ValueAdapterEvent
{
    public InitializedEvent() : base()
    {
    }
}
