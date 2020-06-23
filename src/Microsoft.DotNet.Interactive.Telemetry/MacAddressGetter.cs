// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Microsoft.DotNet.Interactive.Telemetry
{
    internal static class MacAddressGetter
    {
        private const string InvalidMacAddress = "00-00-00-00-00-00";
        private const string MacRegex = @"(?:[a-z0-9]{2}[:\-]){5}[a-z0-9]{2}";
        private const string ZeroRegex = @"(?:00[:\-]){5}00";
        private const int ErrorFileNotFound = 0x2;

        public static string GetMacAddress()
        {
            try
            {
                var macAddress = GetMacAddressCore();
                if (string.IsNullOrWhiteSpace(macAddress) || macAddress.Equals(InvalidMacAddress, StringComparison.OrdinalIgnoreCase))
                {
                    return GetMacAddressByNetworkInterface();
                }

                return macAddress;
            }
            catch
            {
                return null;
            }
        }

        private static string GetMacAddressCore()
        {
            try
            {
                var shellOutput = GetShellOutMacAddressOutput();
                if (string.IsNullOrWhiteSpace(shellOutput))
                {
                    return null;
                }

                return ParseMACAddress(shellOutput);
            }
            catch (Win32Exception e)
            {
                if (e.NativeErrorCode == ErrorFileNotFound)
                {
                    return GetMacAddressByNetworkInterface();
                }

                throw;
            }
        }

        private static string ParseMACAddress(string shelloutput)
        {
            string macAddress = null;
            foreach (Match match in Regex.Matches(shelloutput, MacRegex, RegexOptions.IgnoreCase))
            {
                if (!Regex.IsMatch(match.Value, ZeroRegex))
                {
                    macAddress = match.Value;
                    break;
                }
            }

            return macAddress;
        }

        private static string GetIpCommandOutput()
        {
            var ipResult = new ProcessStartInfo
            {
                FileName = "ip",
                Arguments = "link",
                UseShellExecute = false
            }.ExecuteAndCaptureOutput(out var ipStdOut, out _);

            return ipResult == 0 ? ipStdOut : null;
        }

        private static string GetShellOutMacAddressOutput()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var result = new ProcessStartInfo
                {
                    FileName = "getmac.exe",
                    UseShellExecute = false
                }.ExecuteAndCaptureOutput(out var stdOut, out _);

                return result == 0 ? stdOut : null;
            }
            else
            {
                try
                {
                    var ifconfigResult = new ProcessStartInfo
                    {
                        FileName = "ifconfig",
                        Arguments = "-a",
                        UseShellExecute = false
                    }.ExecuteAndCaptureOutput(out var ifconfigStdOut, out _);

                    if (ifconfigResult == 0)
                    {
                        return ifconfigStdOut;
                    }
                    else
                    {
                        return GetIpCommandOutput();
                    }
                }
                catch (Win32Exception e)
                {
                    if (e.NativeErrorCode == ErrorFileNotFound)
                    {
                        return GetIpCommandOutput();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private static string GetMacAddressByNetworkInterface()
        {
            return GetMacAddressesByNetworkInterface().FirstOrDefault(x => !x.Equals(InvalidMacAddress, StringComparison.OrdinalIgnoreCase));
        }

        private static List<string> GetMacAddressesByNetworkInterface()
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            var macs = new List<string>();

            if (nics.Length < 1)
            {
                macs.Add(string.Empty);
                return macs;
            }

            foreach (var adapter in nics)
            {
                var properties = adapter.GetIPProperties();

                var address = adapter.GetPhysicalAddress();
                var bytes = address.GetAddressBytes();
                macs.Add(string.Join("-", bytes.Select(x => x.ToString("X2"))));
                if (macs.Count >= 10)
                {
                    break;
                }
            }
            return macs;
        }
    }
}
