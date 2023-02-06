// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.SqlServer;

internal class DotnetToolInfo {
    public string PackageId { get; set; }
    public string PackageVersion { get; set; }
    public string CommandName { get; set; }

}

internal static class Utils
{
    /// <summary>
    /// Returns a version of the string quoted with single quotes. Any single quotes in the string are escaped as ''
    /// </summary>
    /// <param name="str">The string to quote</param>
    /// <returns>The quoted string</returns>
    public static string AsSingleQuotedString(this string str)
    {
        return $"'{str.Replace("'", "''")}'";
    }

    /// <summary>
    /// Returns a version of the string quoted with double quotes. Any double quotes in the string are escaped as \"
    /// </summary>
    /// <param name="str">The string to quote</param>
    /// <returns>The quoted string</returns>
    public static string AsDoubleQuotedString(this string str)
    {
        return $"\"{str.Replace("\"", "\\\"")}\"";
    }

    public static async Task<IEnumerable<DotnetToolInfo>> GetGlobalToolListAsync()
    {
        var dotnet = new Dotnet();
        var result = await dotnet.Execute("tool list --global");
        if (result.ExitCode != 0)
        {
            return new DotnetToolInfo[0];
        }

        // Output of dotnet tool list is:
        // Package Id        Version      Commands
        // -------------------------------------------
        // dotnettry.p1      1.0.0        dotnettry.p1

        string[] separator = new[] { " " };
        return result.Output
            .Skip(2)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s =>
            {
                var parts = s.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                return new DotnetToolInfo()
                {
                    PackageId = parts[0],
                    PackageVersion = parts[1],
                    CommandName = parts[2]
                };
            });
    }

    public static async Task CheckAndInstallGlobalToolAsync(string toolName, string minimumVersion, string nugetPackage)
    {
        var installedGlobalTools = await Utils.GetGlobalToolListAsync();
        var expectedVersion = Version.Parse(minimumVersion);
        var installNeeded = true;
        var updateNeeded = false;
        foreach (var tool in installedGlobalTools)
        {
            if (string.Equals(tool.CommandName, toolName, StringComparison.InvariantCultureIgnoreCase))
            {
                installNeeded = false;
                var installedVersion = Version.Parse(tool.PackageVersion);
                if (installedVersion < expectedVersion)
                {
                    updateNeeded = true;
                }
                break;
            }
        }

        var dotnet = new Dotnet();
        if (updateNeeded)
        {
            var commandLineResult = await dotnet.Execute($"tool update --global \"{nugetPackage}\" --version \"{minimumVersion}\"");
            commandLineResult.ThrowOnFailure();
        }
        else if (installNeeded)
        {
            var commandLineResult = await dotnet.Execute($"tool install --global \"{nugetPackage}\" --version \"{minimumVersion}\"");
            commandLineResult.ThrowOnFailure();
        }
    }
}