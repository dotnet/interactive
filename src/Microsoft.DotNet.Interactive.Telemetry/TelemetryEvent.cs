// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.DotNet.Interactive.Telemetry;

public class TelemetryEvent
{
    public TelemetryEvent(
        string eventName = null,
        IDictionary<string, string> properties = null,
        IDictionary<string, double> metrics = null)
    {
        EventName = eventName;
        Properties = properties;
        Metrics = metrics;
    }

    public string EventName { get; }
    public IDictionary<string, string> Properties { get; }
    public IDictionary<string, double> Metrics { get; }
}