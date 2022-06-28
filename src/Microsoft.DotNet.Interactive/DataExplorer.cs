// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.DotNet.Interactive.Formatting.TabularData;

namespace Microsoft.DotNet.Interactive
{
    public static class DataExplorer
    {
        private static ConcurrentDictionary<Type, HashSet<Type>> Explorers = new();

        private static ConcurrentDictionary<Type, Type> DefaultExplorer = new();

        static DataExplorer()
        {
            ResetToDefault();
        }

        public static void ResetToDefault()
        {
            Explorers = new();

            DefaultExplorer = new();

            Register<TabularDataResource, TabularDataResourceSummaryExplorer>();
            SetDefault<TabularDataResource, TabularDataResourceSummaryExplorer>();
        }

        public static DataExplorer<TData> Create<TData>(string dataExplorerTypeName, TData data)
        {
            if (Explorers.TryGetValue(typeof(TData), out var types))
            {
                var explorerType = types.FirstOrDefault(t => t.Name == dataExplorerTypeName);
                if (explorerType is null)
                {
                    throw new InvalidOperationException($"DataType {typeof(TData)} have no DataExplorers defined.");
                }

                return Activator.CreateInstance(explorerType, data) as DataExplorer<TData>;
            }
            throw new InvalidOperationException($"DataType {typeof(TData)} have no DataExplorers defined.");
        }

        public static DataExplorer<TData> CreateDefault<TData>(TData data)
        {
            if (Explorers.TryGetValue(typeof(TData), out var types))
            {
                var explorerType = types.FirstOrDefault();
                if (DefaultExplorer.TryGetValue(typeof(TData), out var defaultExplorerName))
                {
                    explorerType = defaultExplorerName;
                }
                if (explorerType is null)
                {
                    throw new InvalidOperationException($"DataType {typeof(TData)} have no DataExplorers defined.");
                }

                return Activator.CreateInstance(explorerType, data) as DataExplorer<TData>;
            }
            throw new InvalidOperationException($"DataType {typeof(TData)} have no DataExplorers defined.");
        }

        public static void SetDefault<TData, TExplorer>() where TExplorer : DataExplorer<TData>
        {
            DefaultExplorer.AddOrUpdate(typeof(TData), typeof(TExplorer), (_, _) => typeof(TExplorer));
        }

        public static void Register(Type dataType, Type dataExplorerType)
        {
            Explorers.AddOrUpdate(dataType, new HashSet<Type> { dataExplorerType }, (_, types) =>
            {
                types.Add(dataExplorerType);
                return types;
            });
        }

        public static void Register<TData, TExplorer>() where TExplorer : DataExplorer<TData>
        {
            Register(typeof(TData), typeof(TExplorer));
        }
    }
}