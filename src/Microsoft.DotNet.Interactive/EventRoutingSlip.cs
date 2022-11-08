// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public class EventRoutingSlip : RoutingSlip
{

    public EventRoutingSlip(RoutingSlip source = null) : base(source)
    {

    }

    public override void Stamp(Uri uri)
    {
        if (Entries.SingleOrDefault(entry => entry.Uri == uri.AbsoluteUri) is null)
        {
            Entries.Add(new Entry { Uri = uri.AbsoluteUri, Completed = true });
        }
        else
        {
            throw new InvalidOperationException($"The uri {uri} is already in the routing slip");
        }
    }
}