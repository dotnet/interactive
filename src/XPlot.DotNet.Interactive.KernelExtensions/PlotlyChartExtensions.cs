// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.DotNet.Interactive;


namespace XPlot.DotNet.Interactive.KernelExtensions
{
    public static class PlotlyChartExtensions
    {
        public static T UseXplot<T>(this T kernel)
            where T : Kernel
        {
            var extension = new KernelExtension();
            extension.OnLoadAsync(kernel);
            return kernel;
        }
    }
}
