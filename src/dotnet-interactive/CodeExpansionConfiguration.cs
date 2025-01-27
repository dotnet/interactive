// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Jupyter;

namespace Microsoft.DotNet.Interactive.App;

public class CodeExpansionConfiguration
{
    private readonly IJupyterKernelSpecModule _kernelSpecModule;
    private readonly Dictionary<string, CodeExpansion> _codeExpansions = new();
    private bool _checkedKernelSpecs;

    public CodeExpansionConfiguration(
        IEnumerable<CodeExpansion> codeExpansions = null,
        IJupyterKernelSpecModule kernelSpecModule = null)
    {
        _kernelSpecModule = kernelSpecModule;
        if (codeExpansions is not null)
        {
            foreach (var codeExpansion in codeExpansions)
            {
                _codeExpansions.Add(codeExpansion.Info.Name, codeExpansion);
            }
        }
    }

    public Func<RecentConnectionList> GetRecentConnections { get; init; }

    public Action<RecentConnectionList> SaveRecentConnections { get; init; }

    public void AddCodeExpansion(CodeExpansion codeExpansion)
    {
        _codeExpansions.Add(codeExpansion.Info.Name, codeExpansion);
    }

    public async Task<CodeExpansion> GetCodeExpansionAsync(string name)
    {
        await EnsureKernelSpecsHaveBeenCheckedAsync();

        return _codeExpansions.GetValueOrDefault(name);
    }

    public async Task<List<CodeExpansionInfo>> GetCodeExpansionInfosAsync()
    {
        List<CodeExpansionInfo> infos = new();

        await EnsureKernelSpecsHaveBeenCheckedAsync();

        AddExpansionsForRecentConnections();

        infos.AddRange(_codeExpansions.Values.Select(c => c.Info));

        return infos;

        void AddExpansionsForRecentConnections()
        {
            if (GetRecentConnections is not null)
            {
                var recentConnectionList = GetRecentConnections();
                infos.AddRange(recentConnectionList.Select(i => i.Info));
            }
        }
    }

    private async Task EnsureKernelSpecsHaveBeenCheckedAsync()
    {
        if (_checkedKernelSpecs || _kernelSpecModule is null)
        {
            return;
        }

        var kernelSpecs = await _kernelSpecModule.ListKernelsAsync();

        foreach (var kernelSpec in kernelSpecs)
        {
            var codeExpansionInfo = new CodeExpansionInfo(
                kernelSpec.Key,
                CodeExpansion.CodeExpansionKind.KernelSpecConnection,
                kernelSpec.Value.DisplayName);

            var codeExpansion = new CodeExpansion(
                [new($"#!connect jupyter --kernel-name {kernelSpec.Key} --kernel-spec {kernelSpec.Key}", "csharp")],
                codeExpansionInfo);

            _codeExpansions.Add(
                codeExpansionInfo.Name,
                codeExpansion);
        }

        _checkedKernelSpecs = true;
    }
}