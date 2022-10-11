// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive;

public class RoutingSlip 
{
    private readonly HashSet<Uri> _uniqueUris ;
    private readonly List<Uri> _uris;
    private readonly object _lock = new();

    public RoutingSlip(RoutingSlip source = null)
    {
        if (source is { })
        {
            _uniqueUris = new HashSet<Uri>(source._uniqueUris);
            _uris = new List<Uri>(source._uris);
        }
        else
        {
            _uniqueUris = new HashSet<Uri>();
            _uris = new List<Uri>();
        }
    }

    public void MarkAsReceived(Uri kernelOrKernelHostUri)
    {
        lock (_lock)
        {
            if (_uniqueUris.Add(kernelOrKernelHostUri))
            {
                _uris.Add(kernelOrKernelHostUri);
                
            }
            else
            {
                throw new InvalidOperationException($"The routing slip already contains {kernelOrKernelHostUri}");
            }
        }
        
    }
    
    public bool StartsWith(RoutingSlip other)
    {
        return StartsWith(other._uris.ToArray());
       
    }

    public bool StartsWith(params Uri[] kernelUris)
    {
        if (kernelUris.Length > _uris.Count)
        {
            return false;
        }
        var contains = _uris.Zip(kernelUris, (o, i) => o.Equals(i)).All(x => x);
        return contains;
    }
    
    public void Append(RoutingSlip other)
    {
        throw new NotImplementedException();
    }

    public Uri[] ToUriArray()
    {
        return _uris.ToArray();
    }

    public void MarkAsCompleted(Uri uri)
    {
        throw new NotImplementedException();
    }
}
