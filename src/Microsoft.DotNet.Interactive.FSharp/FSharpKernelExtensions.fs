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
    static member UseExtraNamespacesForTesting(kernel: FSharpKernelBase) =
        let code = 
            [
                // opens some System namespaces for testing 
                openNamespaceOrType typeof<System.Threading.Tasks.Task>.Namespace
                openNamespaceOrType typeof<System.Linq.Enumerable>.Namespace

                // opens Microsoft.Microsoft.AspNet.Core.Html for testing
                openNamespaceOrType typeof<IHtmlContent>.Namespace

                // opens Microsoft.DotNet.Interactive.FSharp.FSharpKernelHelpers.Html for testing
                //    note this has some AutoOpen modules inside
                openNamespaceOrType (typeof<FSharpKernelHelpers.IMarker>.Namespace + "." + nameof(FSharpKernelHelpers.Html))

                // opens XPlot.Plotly for testing
                openNamespaceOrType typeof<PlotlyChart>.Namespace

            ] |> String.concat Environment.NewLine

        kernel.DeferCommand(SubmitCode code)
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

    [<Extension>]
    static member UseWho(kernel: FSharpKernelBase) =
        let detailedName = "#!whos"
        let command = Command(detailedName, "Display the names of the current top-level variables and their values.")
        command.Handler <- CommandHandler.Create(
            fun (parseResult: ParseResult) (context: KernelInvocationContext) ->
                let detailed = parseResult.CommandResult.Command.Name = detailedName
                match context.Command with
                | :? SubmitCode ->
                    match context.HandlingKernel with
                    | :? FSharpKernelBase as kernel ->
                        let kernelVariables = kernel.GetCurrentVariables()
                        let currentVariables = CurrentVariables(kernelVariables, detailed)
                        let html = currentVariables.ToDisplayString(HtmlFormatter.MimeType)
                        context.Publish(DisplayedValueProduced(html, context.Command, [| FormattedValue(HtmlFormatter.MimeType, html) |]))
                    | _ -> ()
                | _ -> ()
                Task.CompletedTask)
        command.AddAlias("#!who")
        kernel.AddDirective(command)
        Formatter.Register(CurrentVariablesFormatter())
        kernel
