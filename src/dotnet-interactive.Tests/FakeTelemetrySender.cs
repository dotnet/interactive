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
    private static readonly string _configuredConnectionString = Environment.GetEnvironmentVariable("DOTNET_INTERACTIVE_APPINSIGHTS_CONNECTION_STRING");
    private readonly bool _sendRealTelemetryEvents;

    public FakeTelemetrySender(IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel = null) :
        base(
            _productVersion,
            firstTimeUseNoticeSentinel ?? new FakeFirstTimeUseNoticeSentinel
            {
                SentinelExists = true
            },
            appInsightsConnectionString: GetInstrumentationKey())
    {
        if (_configuredConnectionString is not null)
        {
            _sendRealTelemetryEvents = true;
        }
    }

    private static string GetInstrumentationKey() =>
        _configuredConnectionString ?? 
        $"InstrumentationKey={Guid.NewGuid()};IngestionEndpoint=https://centralus-0.in.applicationinsights.azure.com/;LiveEndpoint=https://centralus.livediagnostics.monitor.azure.com/";

    protected override void DoTrackEvent(
        string eventName,
        IDictionary<string, string> properties = null,
        IDictionary<string, double> metrics = null)
    {
        lock (_telemetryEvents)
        {
            _telemetryEvents.Add(
                new TelemetryEvent(
                    eventName,
                    properties,
                    metrics));
        }

        if (_sendRealTelemetryEvents)
        {
            base.DoTrackEvent(eventName, properties, metrics);
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