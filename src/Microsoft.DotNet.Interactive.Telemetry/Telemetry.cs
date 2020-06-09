// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.DotNet.PlatformAbstractions;

namespace Microsoft.DotNet.Interactive.Telemetry
{
    public sealed class Telemetry : ITelemetry
    {
        internal static string CurrentSessionId = null;
        private TelemetryClient _client = null;
        private Dictionary<string, string> _commonProperties = null;
        private Dictionary<string, double> _commonMeasurements = null;
        private Task _trackEventTask = null;

        private const string InstrumentationKey = "b0dafad5-1430-4852-bc61-95c836b3e612";
        public const string TelemetryOptout = "DOTNET_TRY_CLI_TELEMETRY_OPTOUT";

        public const string WelcomeMessage = @"Welcome to .NET Interactive!
---------------------
Telemetry
---------
The .NET Core tools collect usage data in order to help us improve your experience.The data is anonymous and doesn't include command-line arguments. The data is collected by Microsoft and shared with the community. You can opt-out of telemetry by setting the DOTNET_TRY_CLI_TELEMETRY_OPTOUT environment variable to '1' or 'true' using your favorite shell.
";

        public Telemetry(
            string productVersion,
            IFirstTimeUseNoticeSentinel sentinel,
            string sessionId = null,
            bool blockThreadInitialization = false)
        {
            Enabled = !GetEnvironmentVariableAsBool(TelemetryOptout) && PermissionExists(sentinel);

            if (!Enabled)
            {
                return;
            }

            // Store the session ID in a static field so that it can be reused
            CurrentSessionId = sessionId ?? Guid.NewGuid().ToString();

            if (blockThreadInitialization)
            {
                InitializeTelemetry(productVersion);
            }
            else
            {
                //initialize in task to offload to parallel thread
                _trackEventTask = Task.Factory.StartNew(() => InitializeTelemetry(productVersion));
            }
        }

        public bool Enabled { get; }

        public static bool SkipFirstTimeExperience => GetEnvironmentVariableAsBool(FirstTimeUseNoticeSentinel.SkipFirstTimeExperienceEnvironmentVariableName, false);

        public static bool IsRunningInDockerContainer => GetEnvironmentVariableAsBool("DOTNET_RUNNING_IN_CONTAINER", false);

        private static bool GetEnvironmentVariableAsBool(string name, bool defaultValue = false)
        {
            var str = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(str))
            {
                return defaultValue;
            }

            switch (str.ToLowerInvariant())
            {
                case "true":
                case "1":
                case "yes":
                    return true;
                case "false":
                case "0":
                case "no":
                    return false;
                default:
                    return defaultValue;
            }
        }

        private bool PermissionExists(IFirstTimeUseNoticeSentinel sentinel)
        {
            if (sentinel == null)
            {
                return false;
            }

            return sentinel.Exists();
        }

        public void TrackEvent(string eventName, IDictionary<string, string> properties,
            IDictionary<string, double> measurements)
        {
            if (!Enabled)
            {
                return;
            }

            //continue task in existing parallel thread
            _trackEventTask = _trackEventTask.ContinueWith(
                x => TrackEventTask(eventName, properties, measurements)
            );
        }

        private void ThreadBlockingTrackEvent(string eventName, IDictionary<string, string> properties, IDictionary<string, double> measurements)
        {
            if (!Enabled)
            {
                return;
            }
            TrackEventTask(eventName, properties, measurements);
        }

        private void InitializeTelemetry(string productVersion)
        {
            try
            {
                var config = new TelemetryConfiguration(InstrumentationKey);
                _client = new TelemetryClient(config);
                _client.Context.Session.Id = CurrentSessionId;
                _client.Context.Device.OperatingSystem = RuntimeEnvironment.OperatingSystem;

                _commonProperties = new TelemetryCommonProperties(productVersion).GetTelemetryCommonProperties();
                _commonMeasurements = new Dictionary<string, double>();
            }
            catch (Exception e)
            {
                _client = null;
                // we don't want to fail the tool if telemetry fails.
                Debug.Fail(e.ToString());
            }
        }

        private void TrackEventTask(
            string eventName,
            IDictionary<string, string> properties,
            IDictionary<string, double> measurements)
        {
            if (_client == null)
            {
                return;
            }

            try
            {
                Dictionary<string, string> eventProperties = GetEventProperties(properties);
                Dictionary<string, double> eventMeasurements = GetEventMeasures(measurements);

                _client.TrackEvent(PrependProducerNamespace(eventName), eventProperties, eventMeasurements);
                _client.Flush();
            }
            catch (Exception e)
            {
                Debug.Fail(e.ToString());
            }
        }

        private static string PrependProducerNamespace(string eventName)
        {
            return "dotnet/try/cli/" + eventName;
        }

        private Dictionary<string, double> GetEventMeasures(IDictionary<string, double> measurements)
        {
            Dictionary<string, double> eventMeasurements = new Dictionary<string, double>(_commonMeasurements);
            if (measurements != null)
            {
                foreach (KeyValuePair<string, double> measurement in measurements)
                {
                    eventMeasurements[measurement.Key] = measurement.Value;
                }
            }
            return eventMeasurements;
        }

        private Dictionary<string, string> GetEventProperties(IDictionary<string, string> properties)
        {
            if (properties != null)
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
    }
}
