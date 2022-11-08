// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public class CommandRoutingSlip : RoutingSlip
{

    public CommandRoutingSlip(RoutingSlip source = null) : base(source)
    {

    }

    public override void Stamp(Uri uri)
    {
        if (Entries.SingleOrDefault(e => e.Uri == uri.AbsoluteUri) is {Completed: false} entry)
        {
            entry.Completed = true;
        }
        else
        {
            throw new InvalidOperationException($"The uri {uri} is not in the routing slip or has already been completed");
        }
    }

    public void StampAsArrived(Uri uri)
    {
        if (Entries.SingleOrDefault(entry => entry.Uri == uri.AbsoluteUri) is null)
        {
            Entries.Add(new Entry { Uri = uri.AbsoluteUri, Completed = false });
        }
        else
        {
            throw new InvalidOperationException($"The uri {uri} is already in the routing slip");
        }
    }
}