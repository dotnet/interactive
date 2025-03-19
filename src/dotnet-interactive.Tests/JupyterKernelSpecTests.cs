// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.CommandLine.IO;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.DotNet.Interactive.Utility;
using static Microsoft.DotNet.Interactive.Tests.Utility.DirectoryUtility;

namespace Microsoft.DotNet.Interactive.App.Tests;

[TestClass]
public class JupyterKernelSpecTests
{
    private readonly List<DirectoryInfo> _kernelInstallations = new();

    private TestConsole Console { get; } = new();


    [TestMethod]
    public async Task Returns_success_output_when_kernel_installation_succeeded()
    {
        var kernelDir = CreateDirectory();
        UnpackKernelsSpecTo(kernelDir);

        var jupyterKernelSpecModuleSimulator = new JupyterKernelSpecModuleSimulator(true);
        var kernelSpecInstaller = new JupyterKernelSpecInstaller(Console, jupyterKernelSpecModuleSimulator);
           

        var result = await kernelSpecInstaller.TryInstallKernelAsync(kernelDir);
        var output = Console.Out.ToString();

        using var scope = new AssertionScope();
        result.Should().BeTrue();
        _kernelInstallations.Add(new DirectoryInfo(kernelDir.Name));
        output.Should().MatchEquivalentOf("*Installing using jupyter kernelspec module.*");
        output.Should().MatchEquivalentOf("*Installed * kernel.");
    }

    [TestMethod]
    public async Task Uses_default_kernel_paths_when_kernelspec_module_is_not_on_path_and_jupyter_is_installed()
    {
        var root = CreateDirectory();
        var kernelDir = new DirectoryInfo(Path.Combine(root.FullName, "source"));
        kernelDir.Create();
        var destination = new DirectoryInfo(Path.Combine(root.FullName, "destination"));
        destination.Create();

        UnpackKernelsSpecTo(kernelDir);

        var jupyterKernelSpecModuleSimulator = new JupyterKernelSpecModuleSimulator(false, destination, new Win32Exception("")
        {
            Source = typeof(System.Diagnostics.Process).FullName
        });
            
        var kernelSpecInstaller = new JupyterKernelSpecInstaller(Console, jupyterKernelSpecModuleSimulator);
        var result = await kernelSpecInstaller.TryInstallKernelAsync(kernelDir);
        var output = Console.Out.ToString();

        using var scope = new AssertionScope();
        result.Should().BeTrue();
        output.Should().Match($"Installing using path { destination.FullName}.*");
    }

    [TestMethod]
    public async Task Fails_to_install_kernels_when_jupyter_is_not_installed()
    {
        var root = CreateDirectory();
        var kernelDir = new DirectoryInfo(Path.Combine(root.FullName, "source"));
        kernelDir.Create();
        var destination = new DirectoryInfo(Path.Combine(root.FullName, "destination"));

        UnpackKernelsSpecTo(kernelDir);

        var jupyterKernelSpecModuleSimulator = new JupyterKernelSpecModuleSimulator(false, destination, new Win32Exception("")
        {
            Source = typeof(System.Diagnostics.Process).FullName
        });

        var defaultPath = jupyterKernelSpecModuleSimulator.GetDefaultKernelSpecDirectory();
        var kernelSpecInstaller = new JupyterKernelSpecInstaller(Console, jupyterKernelSpecModuleSimulator);
        var result = await kernelSpecInstaller.TryInstallKernelAsync(kernelDir);
        var error = Console.Error.ToString();

        using var scope = new AssertionScope();
        result.Should().BeFalse();
        error.Should().Match($"*The kernelspec path {defaultPath.FullName} does not exist.*");
    }

    private void UnpackKernelsSpecTo(DirectoryInfo destination)
    {
        var assembly = typeof(Program).Assembly;
        using (var disposableDirectory = DisposableDirectory.Create())
        {
            using (var resourceStream = assembly.GetManifestResourceStream("dotnetKernel.zip"))
            {
                var zipPath = Path.Combine(disposableDirectory.Directory.FullName, "dotnetKernel.zip");

                using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }

                var dotnetDirectory = disposableDirectory.Directory;
                ZipFile.ExtractToDirectory(zipPath, dotnetDirectory.FullName);

                var source = dotnetDirectory.GetDirectories().First();
                // First create all of the directories
                foreach (var dirPath in Directory.GetDirectories(source.FullName, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(source.FullName, destination.FullName));
                }

                // Copy all the files
                foreach (var newPath in Directory.GetFiles(source.FullName, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(source.FullName, destination.FullName));
                }
            }
        }
    }
}