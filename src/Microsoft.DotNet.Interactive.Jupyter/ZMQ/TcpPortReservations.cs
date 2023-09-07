// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Jupyter.ZMQ;

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Linq;
using System.Net;

internal class TcpPortReservations : IDisposable
{
    private readonly List<TcpListener> _reservations;
    private int[] _ports;

    private TcpPortReservations(List<TcpListener> reservations)
    {
        _reservations = reservations;
    }

    public void Dispose()
    {
        FreeReservations();
    }

    public static TcpPortReservations ReserveFreePorts(int numberOfPortsToReserve)
    {
        List<TcpListener> reservations = new();

        for (int i = 0; i < numberOfPortsToReserve; i++)
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            reservations.Add(l);
        }

        return new TcpPortReservations(reservations);
    }

    public void FreeReservations()
    {
        if (_reservations is not null)
        {
            foreach (var listener in _reservations)
            {
                listener.Stop();
            }

            _reservations.Clear();
        }
    }

    public int[] Ports => _ports = _reservations.Select(r => ((IPEndPoint)r.LocalEndpoint).Port).ToArray();
}

