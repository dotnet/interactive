// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Management.Automation;
using System.Reflection;

namespace Microsoft.DotNet.Interactive.PowerShell.Host
{
    internal static class DollarProfileHelper
    {
        public delegate string GetFullProfileFileNameDelegate(
            string hostId,
            bool forCurrentUser);

        private static Lazy<GetFullProfileFileNameDelegate> s_LazyGetFullProfileFileName =
            new Lazy<GetFullProfileFileNameDelegate>(() =>
            {
                // Grab GetFullProfileName static method which handles a lot of the profile
                // path generation.
                MethodInfo method = typeof(HostUtilities).GetMethod(
                    "GetFullProfileFileName",
                    BindingFlags.Static | BindingFlags.NonPublic,
                    null,
                    new [] { typeof(string), typeof(bool) },
                    null);

                return (GetFullProfileFileNameDelegate)method.CreateDelegate(typeof(GetFullProfileFileNameDelegate));
            });

        public static GetFullProfileFileNameDelegate GetFullProfileFileName = s_LazyGetFullProfileFileName.Value;

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
