// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.Interactive.Telemetry;

public class TelemetrySender : ITelemetrySender
{
    private readonly IFirstTimeUseNoticeSentinel _firstTimeUseNoticeSentinel;
    private readonly string _eventsNamespace;
    private readonly string _appInsightsConnectionString;
    private readonly string _currentSessionId = null;
    private TelemetryClient _client = null;
    private Dictionary<string, string> _commonProperties = null;
    private Dictionary<string, double> _commonMetrics = null;
    private Task _trackEventTask = null;

    private const string DefaultAppInsightsConnectionString = "InstrumentationKey=b0dafad5-1430-4852-bc61-95c836b3e612;IngestionEndpoint=https://centralus-0.in.applicationinsights.azure.com/;LiveEndpoint=https://centralus.livediagnostics.monitor.azure.com/";
    public const string TelemetryOptOutEnvironmentVariableName = "DOTNET_INTERACTIVE_CLI_TELEMETRY_OPTOUT";

    public const string WelcomeMessage = $"""
        Welcome to .NET Interactive!
        ---------------------
        Telemetry
        ---------
        The .NET Core tools collect usage data in order to help us improve your experience. The data is anonymous and doesn't include command-line arguments. The data is collected by Microsoft and shared with the community. You can opt-out of telemetry by setting the {TelemetryOptOutEnvironmentVariableName} environment variable to '1' or 'true' using your favorite shell.

        """;

    public TelemetrySender(
        string productVersion,
        IFirstTimeUseNoticeSentinel firstTimeUseNoticeSentinel,
        string eventsNamespace = "dotnet/interactive/cli",
        string appInsightsConnectionString = null)
    {
        if (string.IsNullOrWhiteSpace(eventsNamespace))
        {
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(eventsNamespace));
        }

        _firstTimeUseNoticeSentinel = firstTimeUseNoticeSentinel ?? throw new ArgumentNullException(nameof(firstTimeUseNoticeSentinel));
        _eventsNamespace = eventsNamespace;
        _appInsightsConnectionString = appInsightsConnectionString ?? DefaultAppInsightsConnectionString;

        Enabled = !GetEnvironmentVariableAsBool(TelemetryOptOutEnvironmentVariableName) &&
                  firstTimeUseNoticeSentinel.Exists();

        if (Enabled)
        {
            // Store the session ID in a static field so that it can be reused
            _currentSessionId = Guid.NewGuid().ToString();

            //initialize in task to offload to parallel thread
            _trackEventTask = Task.Factory.StartNew(() => InitializeTelemetry(productVersion));
        }
    }

    public bool Enabled { get; }

    public static bool SkipFirstTimeExperience => GetEnvironmentVariableAsBool(FirstTimeUseNoticeSentinel.SkipFirstTimeExperienceEnvironmentVariableName);

    public static bool IsRunningInDockerContainer => GetEnvironmentVariableAsBool("DOTNET_RUNNING_IN_CONTAINER");

    private static bool GetEnvironmentVariableAsBool(string name)
    {
        switch (Environment.GetEnvironmentVariable(name)?.ToLowerInvariant())
        {
            case "true":
            case "1":
            case "yes":
                return true;

            default:
                return false;
        }
    }

    public void TrackEvent(
        string eventName,
        IDictionary<string, string> properties = null,
        IDictionary<string, double> measurements = null)
    {
        if (!Enabled)
        {
            return;
        }

        //continue task in existing parallel thread
        _trackEventTask = _trackEventTask.ContinueWith(
            _ => DoTrackEvent(eventName, properties, measurements));
    }

    public async Task FlushedAsync() => await _trackEventTask;

    public void TrackStartupEvent(ParseResult parseResult, StartupTelemetryEventBuilder eventBuilder)
    {
        if (parseResult is null || !Enabled || eventBuilder is null)
        {
            return;
        }

        foreach (var entry in eventBuilder.GetTelemetryEventsFrom(parseResult))
        {
            TrackEvent(entry.EventName, entry.Properties, entry.Metrics);
        }
    }

    protected virtual void InitializeTelemetry(string productVersion)
    {
        try
        {
            var config = new TelemetryConfiguration();
            config.ConnectionString = _appInsightsConnectionString;
            _client = new TelemetryClient(config);
            _client.Context.Session.Id = _currentSessionId;
            _client.Context.Device.OperatingSystem = RuntimeEnvironment.OperatingSystem;

            _commonProperties = new TelemetryCommonProperties(productVersion).GetTelemetryCommonProperties();
            _commonMetrics = new Dictionary<string, double>();
        }
        catch (Exception e)
        {
            _client = null;
            // we don't want to fail the tool if telemetry fails.
            Debug.Fail(e.ToString());
        }
    }

    protected virtual void DoTrackEvent(
        string eventName,
        IDictionary<string, string> properties = null,
        IDictionary<string, double> metrics = null)
    {
        if (_client is null)
        {
            return;
        }

        try
        {
            var eventProperties = GetEventProperties(properties);
            var eventMetrics = GetEventMetrics(metrics);

            _client.TrackEvent($"{_eventsNamespace}/{eventName}", eventProperties, eventMetrics);
            _client.Flush();
        }
        catch (Exception e)
        {
            Debug.Fail(e.ToString());
        }
    }

    private Dictionary<string, double> GetEventMetrics(IDictionary<string, double> measurements)
    {
        Dictionary<string, double> eventMeasurements = new Dictionary<string, double>(_commonMetrics);
        if (measurements is not null)
        {
            foreach (KeyValuePair<string, double> measurement in measurements)
            {
                eventMeasurements[measurement.Key] = measurement.Value;
            }
        }

        return eventMeasurements;
    }

    private Dictionary<string, string> GetEventProperties(IDictionary<string, string> properties = null)
    {
        if (properties is not null)
        {
            var eventProperties = new Dictionary<string, string>(_commonProperties);
            foreach (KeyValuePair<string, string> property in properties)
            {
                eventProperties[property.Key] = property.Value;
            }

            return eventProperties;
        }
        else
        {
            return _commonProperties;
        }
    }

    public bool FirstTimeUseNoticeSentinelExists() => 
        _firstTimeUseNoticeSentinel.Exists();

    public void CreateFirstTimeUseNoticeSentinelIfNotExists() => 
        _firstTimeUseNoticeSentinel.CreateIfNotExists();
}