﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public class CommandRoutingSlip : RoutingSlip
{
    
    public CommandRoutingSlip(RoutingSlip source = null) : base(source)
    {

    }

    public void StampAs(Uri uri, string tag)
    {
        var absoluteUri = GetAbsoluteUriWithoutQuery(uri);
        var entry = new Entry { Uri = absoluteUri, Tag = tag };
        if (Entries.SingleOrDefault(e => entry.AbsoluteUri == e.AbsoluteUri) is null)
        {
            Entries.Add(entry);
        }
        else
        {
            throw new InvalidOperationException($"The uri {entry.AbsoluteUri} is already in the routing slip");
        }
    }

    public override void Stamp(Uri uri)
    {
        var absoluteUri = GetAbsoluteUriWithoutQuery(uri);
        if (Entries.SingleOrDefault(e => e.Uri == absoluteUri) is null)
        {
            throw new InvalidOperationException($"The uri {absoluteUri} is not in the routing slip");

        }

        if (Entries.SingleOrDefault(e => e.Uri == absoluteUri) is {Tag: null} entry)
        {
            throw new InvalidOperationException($"The uri {entry.AbsoluteUri} is already in the routing slip");
        }

        Entries.Add(new Entry{Uri = absoluteUri} );
    }

    public void StampAsArrived(Uri uri)
    {
        StampAs(uri, "arrived");
    }
}