// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.Interactive.CSharpProject.Tools;

namespace Microsoft.DotNet.Interactive.CSharpProject.Packaging
{
    public interface IPackage
    {
        string Name { get; }
    }

    public interface IHaveADirectory : IPackage
    {
        DirectoryInfo Directory { get; }
    }

    public interface IHaveADirectoryAccessor : IPackage
    {
        IDirectoryAccessor Directory { get; }
    }

    public interface ICanSupportWasm : IPackage
    {
        bool CanSupportWasm { get; }
    }

    public interface ICreateWorkspace : IPackage
    {
        Task<Workspace> CreateWorkspaceAsync();
    }

    public interface ICreateWorkspaceForRun : IPackage, ICreateWorkspace
    {
        Task<Workspace> CreateWorkspaceForRunAsync();
    }

    public interface ICreateWorkspaceForLanguageServices : IPackage, ICreateWorkspace
    {
        Task<Workspace> CreateWorkspaceForLanguageServicesAsync();
    }
}