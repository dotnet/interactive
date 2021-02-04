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
    
    static let referenceFromType (typ: Type) = sprintf "#r \"%s\"" (typ.Assembly.Location.Replace("\\", "/"))
    static let openNamespaceOrType (whatToOpen: string) = sprintf "open %s" whatToOpen

    [<Extension>]
    static member UseDefaultFormatting(kernel: FSharpKernel) =
        let code = 
            [
                referenceFromType typeof<IHtmlContent>
                referenceFromType typeof<Kernel>
                referenceFromType typeof<FSharpKernelHelpers.IMarker>
                referenceFromType typeof<Formatter>

                openNamespaceOrType typeof<System.Console>.Namespace
                openNamespaceOrType typeof<System.IO.File>.Namespace
                openNamespaceOrType typeof<System.Text.StringBuilder>.Namespace
                openNamespaceOrType typeof<Formatter>.Namespace
            ] |> String.concat Environment.NewLine

        kernel.DeferCommand(SubmitCode code)
        kernel

    [<Extension>]
    static member UseDefaultNamespaces(kernel: FSharpKernel) =
        // F# has its own views on what namespaces are open by default in its scripting model
        kernel

    [<Extension>]
    static member UseKernelHelpers(kernel: FSharpKernel) =
        let code = 
            [
                referenceFromType typeof<FSharpKernelHelpers.IMarker>
                
                // opens Microsoft.DotNet.Interactive.FSharp.FSharpKernelHelpers
                //    note this has some AutoOpen content inside
                openNamespaceOrType (typeof<FSharpKernelHelpers.IMarker>.Namespace)
                
            ] |> String.concat Environment.NewLine

        kernel.DeferCommand(SubmitCode code)
        kernel

