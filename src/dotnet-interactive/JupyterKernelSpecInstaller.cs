// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive.Jupyter;
using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.DotNet.Interactive.App;

public class JupyterKernelSpecInstaller : IJupyterKernelSpecInstaller
{
    private readonly IConsole _console;
    private readonly IJupyterKernelSpecModule _kernelSpecModule;

    public JupyterKernelSpecInstaller(IConsole console) : this(console, new JupyterKernelSpecModule())
    {
    }

    public JupyterKernelSpecInstaller(IConsole console, IJupyterKernelSpecModule jupyterKernelSpecModule)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _kernelSpecModule = jupyterKernelSpecModule;
    }


    public async Task<bool> TryInstallKernelAsync(DirectoryInfo sourceDirectory, DirectoryInfo destination = null)
    {
        var kernelDisplayName = GetKernelDisplayName(sourceDirectory);

        if (destination is not null)
        {
            return InstallKernelSpecToDirectory(sourceDirectory, destination, kernelDisplayName);
        }

        try
        {
            var result = await _kernelSpecModule.InstallKernelAsync(sourceDirectory);
            if (result.ExitCode == 0)
            {
                _console.Out.WriteLine("Installing using jupyter kernelspec module.");
                _console.Out.WriteLine($"Installed \"{kernelDisplayName}\" kernel.");
                return true;
            }
        }
        catch (Win32Exception w32e)
        {
            // file not found when executing process
            if (!w32e.Source.Contains(typeof(System.Diagnostics.Process).FullName))
            {
                _console.Error.WriteLine($"Failed to install \"{kernelDisplayName}\" kernel.");
                throw;
            }
        }

        destination = _kernelSpecModule.GetDefaultKernelSpecDirectory();

        return InstallKernelSpecToDirectory(sourceDirectory, destination, kernelDisplayName);
    }

    private bool InstallKernelSpecToDirectory(DirectoryInfo sourceDirectory, DirectoryInfo destination,
        string kernelDisplayName)
    {
        if (!destination.Exists)
        {
            _console.Error.WriteLine($"The kernelspec path {destination.FullName} does not exist.");
            _console.Error.WriteLine($"Failed to install \"{kernelDisplayName}\" kernel.");

            return false;
        }

        _console.Out.WriteLine($"Installing using path {destination.FullName}.");

        var succeeded = CopyKernelSpecFiles(sourceDirectory, destination);
        if (succeeded)
        {
            _console.Out.WriteLine($"Installed \"{kernelDisplayName}\" kernel.");
        }
        else
        {
            _console.Error.WriteLine(
                $"Failed to install \"{kernelDisplayName}\" kernel.");
        }

        return succeeded;
    }

    private string GetKernelDisplayName(DirectoryInfo directory)
    {
        var kernelSpec = directory.GetFiles("kernel.json", SearchOption.AllDirectories).Single();

        var parsed = JsonDocument.Parse(File.ReadAllText(kernelSpec.FullName));
        return parsed.RootElement.GetProperty("display_name").GetString();
    }


    private bool CopyKernelSpecFiles(DirectoryInfo source, DirectoryInfo destination)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (destination is null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        if (!destination.Exists)
        {
            _console.Error.WriteLine($"Directory {destination.FullName} does not exist.");
            return false;
        }

        try
        {
            var kernelSpecDestinationPath = new DirectoryInfo(Path.Combine(destination.FullName, source.Name));
            if (kernelSpecDestinationPath.Exists)
            {
                kernelSpecDestinationPath.Delete(true);
            }

            kernelSpecDestinationPath.Create();

            // First create all of the directories
            foreach (var dirPath in Directory.GetDirectories(source.FullName, "*", SearchOption.AllDirectories))
            {
                Directory.CreateDirectory(dirPath.Replace(source.FullName, kernelSpecDestinationPath.FullName));
            }

            // Copy all the files
            foreach (var newPath in Directory.GetFiles(source.FullName, "*.*", SearchOption.AllDirectories))
            {
                File.Copy(newPath, newPath.Replace(source.FullName, kernelSpecDestinationPath.FullName));
            }
        }
        catch (IOException ioe)
        {
            _console.Error.WriteLine(ioe.Message);
            return false;
        }

        return true;
    }

}