// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive.ExtensionLab.Data;
using Microsoft.DotNet.Interactive.Formatting;
using Microsoft.ML;

namespace Microsoft.DotNet.Interactive.ExtensionLab
{
    public class DataFrameKernelExtension : IKernelExtension
    {
        public Task OnLoadAsync(Kernel kernel)
        {
            RegisterFormatters();
            return Task.CompletedTask;
        }

        private void RegisterFormatters()
        {
            Formatter.Register<IDataView>((dataView, writer) =>
            {
                var tabularData = dataView.ToTabularJsonString();
                writer.Write(tabularData.ToString());
            }, TabularDataFormatter.MimeType);
        }
    }
}