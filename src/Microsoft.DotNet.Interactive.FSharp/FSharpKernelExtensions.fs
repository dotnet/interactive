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

open FSharp.Reflection
open Internal.Utilities.StructuredFormat // from FSharp.Compiler

type IAnyToLayoutCall = 
    abstract AnyToLayout : FormatOptions * obj * Type -> Internal.Utilities.StructuredFormat.Layout
    abstract FsiAnyToLayout : FormatOptions * obj * Type -> Internal.Utilities.StructuredFormat.Layout

type private AnyToLayoutSpecialization<'T>() = 
    interface IAnyToLayoutCall with
        member this.AnyToLayout(options, o : obj, ty : Type) = Internal.Utilities.StructuredFormat.Display.any_to_layout options ((Unchecked.unbox o : 'T), ty)
        member this.FsiAnyToLayout(options, o : obj, ty : Type) = Internal.Utilities.StructuredFormat.Display.fsi_any_to_layout options ((Unchecked.unbox o : 'T), ty)

[<AbstractClass; Extension; Sealed>]
type FSharpKernelExtensions private () =
    
    static let referenceFromType = fun (typ: Type) -> sprintf "#r \"%s\"" (typ.Assembly.Location.Replace("\\", "/"))
    static let openNamespaceOrType = fun (whatToOpen: String) -> sprintf "open %s" whatToOpen

    static let getAnyToLayoutCall ty = 
        let specialized = typedefof<AnyToLayoutSpecialization<_>>.MakeGenericType [| ty |]
        Activator.CreateInstance(specialized) :?> IAnyToLayoutCall
    
    [<Extension>]
    static member UseDefaultFormatting(kernel: FSharpKernelBase) =
        let code = 
            [
                referenceFromType typeof<IHtmlContent>
                referenceFromType typeof<Kernel>
                referenceFromType typeof<FSharpPocketViewTags>
                referenceFromType typeof<PlotlyChart>
                referenceFromType typeof<Formatter>
                openNamespaceOrType typeof<IHtmlContent>.Namespace
                openNamespaceOrType typeof<FSharpPocketViewTags>.FullName
                openNamespaceOrType typeof<FSharpPocketViewTags>.Namespace
                openNamespaceOrType typeof<PlotlyChart>.Namespace
                openNamespaceOrType typeof<Formatter>.Namespace
            ] |> List.reduce(fun x y -> x + Environment.NewLine + y)

        // Register F# any-to-layout box printing as the default plain text printer
        Formatter.Register<obj>(Action<_,_>(fun object writer -> 
            let options = 
              { FormatOptions.Default with 
                    // TODO get these settings from somewhere
                    FormatProvider = fsi.FormatProvider
                    PrintIntercepts = []
                    FloatingPointFormat = fsi.FloatingPointFormat
                    PrintWidth = fsi.PrintWidth 
                    PrintDepth = fsi.PrintDepth
                    PrintLength = fsi.PrintLength
                    PrintSize = fsi.PrintSize
                    ShowProperties = fsi.ShowProperties
                    ShowIEnumerable = fsi.ShowIEnumerable }

            let ty = object.GetType()
            // strip to a static type
            let ty =
                if FSharpType.IsFunction ty then 
                    FSharpType.MakeFunctionType(FSharpType.GetFunctionElements ty)
                elif FSharpType.IsUnion ty then 
                    FSharpType.GetUnionCases(ty).[0].DeclaringType
                else ty
            let anyToLayoutCall = getAnyToLayoutCall ty
            let layout = 
                //match printMode with
                //| PrintDecl ->
                anyToLayoutCall.FsiAnyToLayout(options, object, ty)
                //| PrintExpr -> 
                //    anyToLayoutCall.AnyToLayout(opts, x, ty)
            Display.output_layout options writer layout
            ), "text/plain", addToDefaults = true)

        kernel.DeferCommand(SubmitCode code)
        kernel

    [<Extension>]
    static member UseDefaultNamespaces(kernel: FSharpKernelBase) =
        let code = @"
open System
open System.Text
open System.Threading.Tasks
open System.Linq"
        kernel.DeferCommand(SubmitCode code)
        kernel

    [<Extension>]
    static member UseKernelHelpers(kernel: FSharpKernelBase) =
        let code = 
            [
                referenceFromType typeof<FSharpKernelHelpers.IMarker>
                openNamespaceOrType (typeof<FSharpKernelHelpers.IMarker>.DeclaringType.Namespace + "." + nameof(FSharpKernelHelpers))
            ] |> List.reduce(fun x y -> x + Environment.NewLine + y)

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
