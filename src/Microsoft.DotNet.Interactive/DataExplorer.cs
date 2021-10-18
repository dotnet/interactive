// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.DotNet.Interactive.Formatting;

namespace Microsoft.DotNet.Interactive
{
    public abstract class DataExplorer<TData>
    {
        public string Id { get; } = Guid.NewGuid().ToString("N");

        public TData Data { get; }

        public static List<DataExplorer<TData>> DataExplorers;

        public static Dictionary<Type, List<DataExplorer<TData>>> Explorers;

        public static DataExplorer<TData> Default() {
            return DataExplorers[0]; // Explorers[data][0];
        }

        public static object DefaultType() {
            Type de = typeof(DataExplorer<>);
            Type data = typeof(TData);
            Type constructed = de.MakeGenericType(data);
            return Activator.CreateInstance(constructed); 
        }

        protected DataExplorer(TData data)
        {
            Data = data;
        }

        public static void RegisterFormatters()
        {
            Formatter.Register<DataExplorer<TData>>((explorer, writer) =>
            {
                DataExplorers.Add(explorer);
                if (Explorers.ContainsKey(explorer.GetType())) {
                    var dataExplorers = Explorers[explorer.GetType()];
                    dataExplorers.Add(explorer);
                    Explorers[explorer.GetType()] = dataExplorers;
                } else {
                    List<DataExplorer<TData>> explorers = new List<DataExplorer<TData>>();
                    explorers.Add(explorer);
                    Explorers.Add(explorer.GetType(), explorers);
                }
                explorer.ToHtml().WriteTo(writer, HtmlEncoder.Default);
            }, HtmlFormatter.MimeType);
            
        }

        static DataExplorer()
        {
            RegisterFormatters();
        }

        protected abstract IHtmlContent ToHtml();
    }
}