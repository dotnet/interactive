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
        var absoluteUri = GetAbsoluteUriWithoutQuery(uri);
        var notCompleted = uri.Query.Contains("completed=false");
        if (Entries.SingleOrDefault(e => e.Uri == absoluteUri) is {Completed: false} entry)
        {
            if(notCompleted)
            {
                throw new InvalidOperationException($"The uri {uri} is not valid for this routing slip");
            }
            entry.Completed = true;
        }
        else
        {
            throw new InvalidOperationException($"The uri {uri} is not in the routing slip or has already been completed");
        }
    }

    public void StampAsArrived(Uri uri)
    {
        var absoluteUri = GetAbsoluteUriWithoutQuery(uri);
        if (Entries.SingleOrDefault(entry => entry.Uri == absoluteUri) is null)
        {
            Entries.Add(new Entry { Uri = absoluteUri, Completed = false });
        }
        else
        {
            throw new InvalidOperationException($"The uri {uri} is already in the routing slip");
        }
    }
}