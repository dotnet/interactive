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
    
    static let referenceFromType = fun (typ: Type) -> sprintf "#r \"%s\"" (typ.Assembly.Location.Replace("\\", "/"))
    static let openNamespaceOrType = fun (whatToOpen: String) -> sprintf "open %s" whatToOpen

    [<Extension>]
    static member UseDefaultFormatting(kernel: FSharpKernelBase) =
        let code = 
            [
                referenceFromType typeof<IHtmlContent>
                referenceFromType typeof<Kernel>
                referenceFromType typeof<FSharpPocketViewTags>
                referenceFromType typeof<PlotlyChart>
                referenceFromType typeof<Formatter>
                referenceFromType typeof<FSharp.Compiler.Interactive.InteractiveSession>
                openNamespaceOrType typeof<IHtmlContent>.Namespace
                openNamespaceOrType typeof<FSharpPocketViewTags>.FullName
                openNamespaceOrType typeof<FSharpPocketViewTags>.Namespace
                openNamespaceOrType typeof<PlotlyChart>.Namespace
                openNamespaceOrType typeof<Formatter>.Namespace
            ] |> List.reduce(fun x y -> x + Environment.NewLine + y)

        // Register F# Interactive box printing as the default plain text printer, selectively overriding plaintext settings
        // in DefaultPlainTextFormatterSet
        Formatter.Register<obj>(Action<_,_>(FSharpPlainText.formatObject), "text/plain", addToDefaults = true)
        Formatter.Register<System.Collections.IEnumerable>(Action<_,_>(FSharpPlainText.formatObject), "text/plain", addToDefaults = true)

        // Primitives render well as HTML
        Formatter.Register(Func<int8,string>(fun v -> v.ToString(fsi.FormatProvider).HtmlEncode().ToString()), addToDefaults = true)
        Formatter.Register(Func<int16,string>(fun v -> v.ToString(fsi.FormatProvider).HtmlEncode().ToString()), addToDefaults = true)
        Formatter.Register(Func<int32,string>(fun v -> v.ToString(fsi.FormatProvider).HtmlEncode().ToString()), addToDefaults = true)
        Formatter.Register(Func<int64,string>(fun v -> v.ToString(fsi.FormatProvider).HtmlEncode().ToString()), addToDefaults = true)
        Formatter.Register(Func<nativeint,string>(fun v -> v.ToString().HtmlEncode().ToString()), addToDefaults = true)
        Formatter.Register(Func<uint8,string>(fun v -> v.ToString(fsi.FormatProvider).HtmlEncode().ToString()), addToDefaults = true)
        Formatter.Register(Func<uint16,string>(fun v -> v.ToString(fsi.FormatProvider).HtmlEncode().ToString()), addToDefaults = true)
        Formatter.Register(Func<uint32,string>(fun v -> v.ToString(fsi.FormatProvider).HtmlEncode().ToString()), addToDefaults = true)
        Formatter.Register(Func<uint64,string>(fun v -> v.ToString(fsi.FormatProvider).HtmlEncode().ToString()), addToDefaults = true)
        Formatter.Register(Func<uint64,string>(fun v -> v.ToString(fsi.FormatProvider).HtmlEncode().ToString()), addToDefaults = true)

        // https://github.com/dotnet/interactive/issues/697
        HtmlFormatter.PreformatEmbeddedPlainText <- true

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
