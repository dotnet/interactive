// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Management.Automation;

namespace Microsoft.DotNet.Interactive.PowerShell.Host
{
    internal static class DollarProfileHelper
    {
        private const string _profileName = "Microsoft.dotnet-interactive_profile.ps1";

        public static string GetFullProfileFilePath(bool forCurrentUser)
        {
            if (!forCurrentUser)
            {
                string pshome = Path.GetDirectoryName(typeof(PSObject).Assembly.Location);
                return Path.Combine(pshome, _profileName);
            }

            string configPath;
            if (Platform.IsWindows)
            {
                configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "PowerShell");
            }
            else
            {
                configPath = Platform.SelectProductNameForDirectory(Platform.XDG_Type.CONFIG);
            }

            return Path.Combine(configPath, _profileName);
        }

        public static PSObject GetDollarProfile(string allUsersCurrentHost, string currentUserCurrentHost)
        {
            PSObject returnValue = new PSObject(currentUserCurrentHost);
            returnValue.Properties.Add(new PSNoteProperty("AllUsersCurrentHost", allUsersCurrentHost));
            returnValue.Properties.Add(new PSNoteProperty("CurrentUserCurrentHost", currentUserCurrentHost));

            // TODO: Decide on whether or not we want to support running the AllHosts profiles
            return returnValue;
        }
    }
}
