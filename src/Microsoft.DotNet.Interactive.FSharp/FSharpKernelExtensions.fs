namespace Microsoft.DotNet.Interactive.FSharp

open System
open System.CommandLine
open System.CommandLine.Invocation
open System.CommandLine.Parsing
open System.Runtime.CompilerServices
open System.Threading.Tasks
open Microsoft.AspNetCore.Html
open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.Commands
open Microsoft.DotNet.Interactive.Events
open Microsoft.DotNet.Interactive.FSharp
open Microsoft.DotNet.Interactive.Formatting
open XPlot.Plotly

[<AbstractClass; Extension; Sealed>]
type FSharpKernelExtensions private () =
    
    static let referenceFromType (typ: Type) = sprintf "#r \"%s\"" (typ.Assembly.Location.Replace("\\", "/"))
    static let openNamespaceOrType (whatToOpen: string) = sprintf "open %s" whatToOpen

    [<Extension>]
    static member UseDefaultFormatting(kernel: FSharpKernelBase) =
        let code = 
            [
                referenceFromType typeof<IHtmlContent>
                referenceFromType typeof<Kernel>
                referenceFromType typeof<FSharpKernelHelpers.IMarker>
                referenceFromType typeof<PlotlyChart>
                referenceFromType typeof<Formatter>

                openNamespaceOrType typeof<System.Console>.Namespace
                openNamespaceOrType typeof<System.IO.File>.Namespace
                openNamespaceOrType typeof<System.Text.StringBuilder>.Namespace
                openNamespaceOrType typeof<Formatter>.Namespace
            ] |> String.concat Environment.NewLine

        kernel.DeferCommand(SubmitCode code)
        kernel

    [<Extension>]
    static member UseDefaultNamespaces(kernel: FSharpKernelBase) =
        // F# has its own views on what namespaces are open by default in its scripting model
        kernel

    [<Extension>]
    static member UseKernelHelpers(kernel: FSharpKernelBase) =
        let code = 
            [
                referenceFromType typeof<FSharpKernelHelpers.IMarker>
                
                // opens Microsoft.DotNet.Interactive.FSharp.FSharpKernelHelpers
                //    note this has some AutoOpen modules inside
                openNamespaceOrType (typeof<FSharpKernelHelpers.IMarker>.Namespace)

            ] |> String.concat Environment.NewLine

        kernel.DeferCommand(SubmitCode code)
        kernel

