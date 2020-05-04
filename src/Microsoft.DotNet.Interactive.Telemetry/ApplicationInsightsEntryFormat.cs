// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.DotNet.Interactive.Telemetry
{
    public class ApplicationInsightsEntryFormat
    {
        public ApplicationInsightsEntryFormat(
            string eventName = null,
            IDictionary<string, string> properties = null,
            IDictionary<string, double> measurements = null)
        {
            EventName = eventName;
            Properties = properties;
            Measurements = measurements;
        }

        public string EventName { get; }
        public IDictionary<string, string> Properties { get; }
        public IDictionary<string, double> Measurements { get; }

        public ApplicationInsightsEntryFormat WithAppliedToPropertiesValue(Func<string, string> func)
        {
            var appliedProperties = Properties.ToDictionary(p => p.Key, p => func(p.Value));
            return new ApplicationInsightsEntryFormat(EventName, appliedProperties, Measurements);
        }
    }
}
