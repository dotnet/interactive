// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.Telemetry
{
    public static class TelemetryExtensions
    {
        public static void SendFiltered(this ITelemetry telemetry, ITelemetryFilter filter, object o)
        {
            if (o == null || !telemetry.Enabled || filter == null)
            {
                return;
            }

            foreach (ApplicationInsightsEntryFormat entry in filter.Filter(o))
            {
                telemetry.TrackEvent(entry.EventName, entry.Properties, entry.Measurements);
            }
        }
    }
}
