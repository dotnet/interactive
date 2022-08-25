// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Telemetry;

namespace Microsoft.DotNet.Interactive.App.Tests;

public class FakeTelemetrySender : TelemetrySender
{
    private static readonly string _productVersion = BuildInfo.GetBuildInfo(typeof(Program).Assembly).AssemblyInformationalVersion + "-tests";
    private readonly List<TelemetryEvent> _telemetryEvents = new();

    public FakeTelemetrySender(IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel = null) :
        base(
            _productVersion,
            firstTimeUseNoticeSentinel ?? new FakeFirstTimeUseNoticeSentinel
            {
                SentinelExists = true
            },
            appInsightsConnectionString: GetInstrumentationKey())
    {
    }

    private static string GetInstrumentationKey() =>
        Environment.GetEnvironmentVariable("DOTNET_INTERACTIVE_APPINSIGHTS_CONNECTION_STRING") ?? 
        $@"InstrumentationKey={Guid.NewGuid()};IngestionEndpoint=https://centralus-0.in.applicationinsights.azure.com/;LiveEndpoint=https://centralus.livediagnostics.monitor.azure.com/";

    protected override void DoTrackEvent(
        string eventName,
        IDictionary<string, string> properties,
        IDictionary<string, double> metrics)
    {
        lock (_telemetryEvents)
        {
            _telemetryEvents.Add(
                new TelemetryEvent(
                    eventName,
                    properties,
                    metrics));
        }
    }

    public IReadOnlyList<TelemetryEvent> TelemetryEvents
    {
        get
        {
            Task.Run(FlushedAsync).Wait();
            lock (_telemetryEvents)
            {
                return _telemetryEvents.ToArray();
            }
        }
    }
}