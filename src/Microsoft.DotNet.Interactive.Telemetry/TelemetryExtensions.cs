// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Telemetry
{
    public static class TelemetryExtensions
    {
        public static void SendFiltered(this ITelemetrySender telemetrySender, ITelemetryFilter filter, object o)
        {
            if (o is null || !telemetrySender.Enabled || filter is null)
            {
                return;
            }

            foreach (ApplicationInsightsEntryFormat entry in filter.Filter(o))
            {
                telemetrySender.TrackEvent(entry.EventName, entry.Properties, entry.Measurements);
            }
        }
    }
}
