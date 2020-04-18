// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace Microsoft.DotNet.Interactive.PowerShell
{
    using System.Management.Automation;
    using System.Reflection;

    internal static class DollarProfileHelper
    {
        private const string _profileName = "Microsoft.dotnet-interactive_profile.ps1";

        // This is the easiest way to get the config directory (where the PROFILE lives), unfortunately.
        private static readonly string _configPath = typeof(Platform).GetField(
                "ConfigDirectory",
                BindingFlags.Static | BindingFlags.NonPublic)
            .GetValue(null) as string;

        internal static readonly string AllUsersCurrentHost = GetFullProfileFilePath(forCurrentUser: false);
        internal static readonly string CurrentUserCurrentHost = GetFullProfileFilePath(forCurrentUser: true);

        private static string GetFullProfileFilePath(bool forCurrentUser)
        {
            if (!forCurrentUser)
            {
                string pshome = Path.GetDirectoryName(typeof(PSObject).Assembly.Location);
                return Path.Combine(pshome, _profileName);
            }

            return Path.Combine(_configPath, _profileName);
        }

        public static PSObject GetProfileValue()
        {
            PSObject dollarProfile = new PSObject(CurrentUserCurrentHost);
            dollarProfile.Properties.Add(new PSNoteProperty("AllUsersCurrentHost", AllUsersCurrentHost));
            dollarProfile.Properties.Add(new PSNoteProperty("CurrentUserCurrentHost", CurrentUserCurrentHost));
            // TODO: Decide on whether or not we want to support running the AllHosts profiles

            return dollarProfile;
        }
    }
}
