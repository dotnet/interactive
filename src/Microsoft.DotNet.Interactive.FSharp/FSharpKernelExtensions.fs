// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp

open System
open System.Runtime.CompilerServices
open Microsoft.AspNetCore.Html
open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.Commands
open Microsoft.DotNet.Interactive.FSharp
open Microsoft.DotNet.Interactive.Formatting

[<AbstractClass; Extension; Sealed>]
type FSharpKernelExtensions private () =
    
    static let referenceAssemblyContaining (typ: Type) = sprintf "#r \"%s\"" (typ.Assembly.Location.Replace("\\", "/"))
    static let openNamespaceContaining (typ: Type) = sprintf "open %s" typ.Namespace
    static let openType (typ: Type) = sprintf "open type %s.%s" typ.Namespace typ.Name

    [<Extension>]
    static member UseDefaultFormatting(kernel: FSharpKernel) =
        let code = 
            [
                referenceAssemblyContaining typeof<IHtmlContent>
                referenceAssemblyContaining typeof<Kernel>
                referenceAssemblyContaining typeof<FSharpKernelHelpers.IMarker>
                referenceAssemblyContaining typeof<Formatter>

                openNamespaceContaining typeof<System.Console>
                openNamespaceContaining typeof<System.IO.File>
                openNamespaceContaining typeof<System.Text.StringBuilder>
                openNamespaceContaining typeof<Formatter>
            ] |> String.concat Environment.NewLine

        kernel.DeferCommand(SubmitCode code)
        kernel

    [<Extension>]
    static member UseKernelHelpers(kernel: FSharpKernel) =
        let code = 
            [
                referenceAssemblyContaining typeof<FSharpKernelHelpers.IMarker>
                
                // opens Microsoft.DotNet.Interactive.FSharp.FSharpKernelHelpers
                //    note this has some AutoOpen content inside
                openNamespaceContaining typeof<FSharpKernelHelpers.IMarker>
               
                referenceAssemblyContaining typeof<Kernel>
                openType typeof<Kernel>

            ] |> String.concat Environment.NewLine

        kernel.DeferCommand(SubmitCode code)
        kernel
