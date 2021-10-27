// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;
using System.Linq;

namespace Microsoft.DotNet.Interactive
{
    public abstract class DataExplorer<TData>
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");

        public TData Data { get; }

        protected DataExplorer(TData data)
        {
            Data = data;
        }

        public static void RegisterFormatters()
        {
            Formatter.Register<DataExplorer<TData>>((explorer, writer) =>
            {        
                explorer.ToHtml().WriteTo(writer, HtmlEncoder.Default);
            }, HtmlFormatter.MimeType);
            
        }

        static DataExplorer()
        {
            RegisterFormatters();
        }

        protected abstract IHtmlContent ToHtml();

        public static void Register<TDataExplorer>() where TDataExplorer : DataExplorer<TData>
        {
            DataExplorer.Register(typeof(TData), typeof(TDataExplorer));
        }
    }

    public static class DataExplorer
    {
        private static ConcurrentDictionary<Type, HashSet<Type>> Explorers = new ();

        private static ConcurrentDictionary<Type, string> DefaultExplorer = new ();

        public static DataExplorer<T> Create<T>(string dataExplorerTypeName, T data)
        {
            if (Explorers.TryGetValue(typeof(T), out var types))
            {
                var explorerType = types.FirstOrDefault(t => t.Name == dataExplorerTypeName);
                if (explorerType is null)
                {
                    throw new InvalidOperationException($"DataType {typeof(T)} have no DataExplorers defined.");
                }
                else
                {
                    return Activator.CreateInstance(explorerType, data) as DataExplorer<T>;
                }
            }
            throw new InvalidOperationException($"DataType {typeof(T)} have no DataExplorers defined.");
        }

        public static DataExplorer<T> CreateDefault<T>(T data)
        {
            if (Explorers.TryGetValue(typeof(T), out var types))
            {
                var explorerType = types.FirstOrDefault();
                if (DefaultExplorer.TryGetValue(typeof (T), out var defaultExplorerName))
                {
                    explorerType = types.FirstOrDefault(t => t.Name == defaultExplorerName);
                }
                if (explorerType is null)
                {
                    throw new InvalidOperationException($"DataType {typeof(T)} have no DataExplorers defined.");
                } else
                {
                    return Activator.CreateInstance(explorerType, data) as DataExplorer<T>;
                }
            }
            throw new InvalidOperationException($"DataType {typeof(T)} have no DataExplorers defined.");
        }

        public static void SetDefault<T>(string defaultExplorerName)
        {
            DefaultExplorer.AddOrUpdate(typeof(T), defaultExplorerName, (_, _) => defaultExplorerName);
        }

        internal static void Register(Type dataType, Type dataExplorerType)
        {
            Explorers.AddOrUpdate(dataType, new HashSet<Type> { dataExplorerType }, (_, types) =>
            {
                types.Add(dataExplorerType);
                return types;
            });
        }
    }
}