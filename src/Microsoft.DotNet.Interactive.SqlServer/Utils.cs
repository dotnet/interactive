// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Utility;

namespace Microsoft.DotNet.Interactive.SqlServer;

public class DotnetToolInfo {
    public string PackageId { get; set; }
    public string PackageVersion { get; set; }
    public string CommandName { get; set; }

}

public static class Utils
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
        var args = "tool list --global";
        var result = await dotnet.Execute(args);
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
        var dotnet = new Dotnet();
        var installedGlobalTools = await Utils.GetGlobalToolListAsync();
        var expectedVersion = Version.Parse(minimumVersion);
        bool toolInstalled = installedGlobalTools.Any(tool =>
        {
            if (string.Equals(tool.CommandName, toolName, StringComparison.InvariantCultureIgnoreCase))
            {
                var installedVersion = Version.Parse(tool.PackageVersion);
                return installedVersion >= expectedVersion;
            }
            else
            {
                return false;
            }
        });
        if (!toolInstalled)
        {
            var commandLineResult = await dotnet.ToolInstall(nugetPackage, null, null, minimumVersion);
            commandLineResult.ThrowOnFailure();
        }
    }
}