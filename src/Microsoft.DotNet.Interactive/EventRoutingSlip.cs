// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public class EventRoutingSlip : RoutingSlip
{
    public override void Stamp(Uri uri)
    {
        var absoluteUri = GetAbsoluteUriWithoutQuery(uri);
        if (Entries.SingleOrDefault(entry => entry.AbsoluteUriWithoutQuery == absoluteUri) is null)
        {
            Entries.Add(new Entry(absoluteUri));
        }
        else
        {
            throw new InvalidOperationException($"The uri {absoluteUri} is already in the routing slip");
        }
    }
}