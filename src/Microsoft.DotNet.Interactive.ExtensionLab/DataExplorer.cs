// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.Events;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class DataExplorer
    {
        public DataExplorer(TabularDataResourceJsonString source)
        {
            Id = Guid.NewGuid().ToString("N");
            TabularDataResourceData = source;
        }

        public Task LoadDataAsync(TabularDataResourceJsonString source)
        {
            throw new NotImplementedException();
        }

        public Task<TabularDataResourceJsonString> GetFilteredDataAsync()
        {
            throw new NotImplementedException();
        }

        public string Id { get; }

        public TabularDataResourceJsonString TabularDataResourceData { get; }
    }

    public class SetDataExplorerData : KernelCommand
    {
        [JsonPropertyName("data")]
        public TabularDataResourceJsonString Data { get; }

        [JsonPropertyName("dataExplorerId")]
        public string DataExplorerId { get; }

        public SetDataExplorerData(TabularDataResourceJsonString data, string dataExplorerId, string targetKernelName = null, KernelCommand parent = null) : base(targetKernelName, parent)
        {
            Data = data;
            DataExplorerId = dataExplorerId;
        }
    }

    public class GetDataExplorerData : KernelCommand
    {
        [JsonPropertyName("dataExplorerId")]
        public string DataExplorerId { get; }
        public GetDataExplorerData(string dataExplorerId, string targetKernelName = null, KernelCommand parent = null) : base(targetKernelName, parent)
        {
            DataExplorerId = dataExplorerId;
        }
    }

    public class FilteredDataProduced : KernelEvent
    {
        [JsonPropertyName("dataExplorerId")]
        public string DataExplorerId { get; }

        public TabularDataResourceJsonString Data { get; }

        public FilteredDataProduced(TabularDataResourceJsonString data, string dataExplorerId, KernelCommand command) : base(command)
        {
            Data = data;
            DataExplorerId = dataExplorerId;
        }
    }
}