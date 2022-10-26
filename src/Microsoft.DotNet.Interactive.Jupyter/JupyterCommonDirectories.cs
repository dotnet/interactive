﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.DotNet.Interactive.Jupyter;

// Provides jupyter common directories and file locations 
// See https://docs.jupyter.org/en/latest/use/jupyter-directories.html
internal static class JupyterCommonDirectories
{
    // Default directory for data can be changed based on environment variable for jupyter
    private static string JUPYTER_DATA_DIR = nameof(JUPYTER_DATA_DIR);

    // Additional paths directory for data can be provided via environment variable
    // these are prioritized over defaults 
    private static string JUPYTER_PATH = nameof(JUPYTER_PATH);

    public static DirectoryInfo GetRuntimeDirectory()
    {
        var directory = GetDefaultDataDirectoryFromEnv();
        if (directory == null || !directory.Exists)
        {
            directory = GetDefaultJupyterDataDirectory();
        }

        directory = new DirectoryInfo(Path.Combine(directory.FullName, "runtime"));
        return directory;
    }

    public static IReadOnlyList<DirectoryInfo> GetDataDirectories()
    {
        List<DirectoryInfo> directories = new List<DirectoryInfo>();

        string jupyterPaths = Environment.GetEnvironmentVariable(JUPYTER_PATH);
        if (!string.IsNullOrEmpty(jupyterPaths))
        {
            var jupyterDirPaths = jupyterPaths.Split(Path.PathSeparator);
            foreach (var path in jupyterDirPaths)
            {
                directories.Add(new DirectoryInfo(path));
            }
        }

        var directory = GetDefaultDataDirectoryFromEnv();
        if (directory != null)
        {
            directories.Add(directory);
        }

        directories.Add(GetDefaultJupyterDataDirectory());
        directories.Add(GetDefaultAnacondaDataDirectory());
        directories.Add(new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "jupyter")));

        return directories;
    }

    public static DirectoryInfo GetDataDirectory()
    {
        var directory = GetDefaultDataDirectoryFromEnv();
        if (directory != null && directory.Exists)
        {
            return directory;
        }

        directory = GetDefaultAnacondaDataDirectory();
        if (!directory.Exists)
        {
            directory = GetDefaultJupyterDataDirectory();
        }

        return directory;
    }

    private static DirectoryInfo GetDefaultDataDirectoryFromEnv()
    {
        string dataDirPath = Environment.GetEnvironmentVariable(JUPYTER_DATA_DIR);
        if (!string.IsNullOrEmpty(dataDirPath))
        {
            return new DirectoryInfo(dataDirPath);
        }

        return null;
    }

    private static DirectoryInfo GetDefaultAnacondaDataDirectory()
    {
        DirectoryInfo directory;
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.Win32NT:
            case PlatformID.Unix:
                directory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "anaconda3", "share", "jupyter"));
                break;
            case PlatformID.MacOSX:
                directory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "opt", "anaconda3", "share", "jupyter"));
                break;
            default:
                throw new PlatformNotSupportedException();
        }

        return directory;
    }

    private static DirectoryInfo GetDefaultJupyterDataDirectory()
    {
        DirectoryInfo directory;
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.Win32NT:
                directory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "jupyter"));
                break;
            case PlatformID.Unix:
                directory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "jupyter"));
                break;
            case PlatformID.MacOSX:
                directory = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Jupyter"));
                break;
            default:
                throw new PlatformNotSupportedException();
        }

        return directory;
    }
}