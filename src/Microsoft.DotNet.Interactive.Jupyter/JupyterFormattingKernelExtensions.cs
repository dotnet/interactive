// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.DotNet.Interactive.Commands;
using Microsoft.DotNet.Interactive.CSharp;
using Microsoft.DotNet.Interactive.FSharp;
using Microsoft.DotNet.Interactive.Jupyter.Formatting;

namespace Microsoft.DotNet.Interactive.Jupyter
{
    public static class KernelFormattingExtensions
    {
        public static CSharpKernel UseMathAndLaTeX(this CSharpKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            kernel.DeferCommand(
                new SubmitCode($@"
#r ""{typeof(LaTeXString).Assembly.Location.Replace("\\", "/")}""
using {typeof(LaTeXString).Namespace};
"));

            return kernel;
        }

        public static FSharpKernel UseMathAndLaTeX(this FSharpKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException(nameof(kernel));
            }

            kernel.DeferCommand(
                new SubmitCode($@"
#r ""{typeof(LaTeXString).Assembly.Location.Replace("\\", "/")}""
open {typeof(LaTeXString).Namespace}
"));

            return kernel;
        }
    }
}