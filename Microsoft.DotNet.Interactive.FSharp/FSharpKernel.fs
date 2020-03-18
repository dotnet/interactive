// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp

open System
open System.Collections.Generic
open System.Linq
open System.Runtime.InteropServices
open System.Text
open System.Threading
open System.Threading.Tasks

open Microsoft.CodeAnalysis.Tags
open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.Commands
open Microsoft.DotNet.Interactive.Events
open Microsoft.DotNet.Interactive.Utility

open Interactive.DependencyManager;
open FSharp.Compiler.Interactive.Shell
open FSharp.Compiler.Scripting
open FSharp.Compiler.SourceCodeServices

type FSharpKernel() as this =
    inherit KernelBase("fsharp")

    let resolvedAssemblies = List<string>()
    static let lockObj = Object();

    let variables = HashSet<string>()

    let createScript registerForDisposal  =  
        let script = lock lockObj (fun () -> new FSharpScript(additionalArgs=[|"/langversion:preview"|]))

        let valueBoundHandler = new Handler<(obj * Type * string)>(fun _ (_, _, name) -> variables.Add(name) |> ignore)
        do script.ValueBound.AddHandler valueBoundHandler
        do registerForDisposal(fun () -> script.ValueBound.RemoveHandler valueBoundHandler)

        let handler = new Handler<string> (fun o s -> resolvedAssemblies.Add(s))
        script

    let script = lazy createScript this.RegisterForDisposal
    do base.RegisterForDisposal(fun () -> if script.IsValueCreated then (script.Value :> IDisposable).Dispose())
    let mutable cancellationTokenSource = new CancellationTokenSource()

    let messageMap = Dictionary<string, string>()

    let getLineAndColumn (text: string) offset =
        let rec getLineAndColumn' i l c =
            if i >= offset then l, c
            else
                match text.[i] with
                | '\n' -> getLineAndColumn' (i + 1) (l + 1) 0
                | _ -> getLineAndColumn' (i + 1) l (c + 1)
        getLineAndColumn' 0 1 0

    let kindString (glyph: FSharpGlyph) =
        match glyph with
        | FSharpGlyph.Class -> WellKnownTags.Class
        | FSharpGlyph.Constant -> WellKnownTags.Constant
        | FSharpGlyph.Delegate -> WellKnownTags.Delegate
        | FSharpGlyph.Enum -> WellKnownTags.Enum
        | FSharpGlyph.EnumMember -> WellKnownTags.EnumMember
        | FSharpGlyph.Event -> WellKnownTags.Event
        | FSharpGlyph.Exception -> WellKnownTags.Class
        | FSharpGlyph.Field -> WellKnownTags.Field
        | FSharpGlyph.Interface -> WellKnownTags.Interface
        | FSharpGlyph.Method -> WellKnownTags.Method
        | FSharpGlyph.OverridenMethod -> WellKnownTags.Method
        | FSharpGlyph.Module -> WellKnownTags.Module
        | FSharpGlyph.NameSpace -> WellKnownTags.Namespace
        | FSharpGlyph.Property -> WellKnownTags.Property
        | FSharpGlyph.Struct -> WellKnownTags.Structure
        | FSharpGlyph.Typedef -> WellKnownTags.Class
        | FSharpGlyph.Type -> WellKnownTags.Class
        | FSharpGlyph.Union -> WellKnownTags.Enum
        | FSharpGlyph.Variable -> WellKnownTags.Local
        | FSharpGlyph.ExtensionMethod -> WellKnownTags.ExtensionMethod
        | FSharpGlyph.Error -> WellKnownTags.Error

    let filterText (declarationItem: FSharpDeclarationListItem) =
        match declarationItem.NamespaceToOpen, declarationItem.Name.Split '.' with
        // There is no namespace to open and the item name does not contain dots, so we don't need to pass special FilterText to Roslyn.
        | None, [|_|] -> null
        // Either we have a namespace to open ("DateTime (open System)") or item name contains dots ("Array.map"), or both.
        // We are passing last part of long ident as FilterText.
        | _, idents -> Array.last idents

    let documentation (declarationItem: FSharpDeclarationListItem) =
        declarationItem.DescriptionText.ToString()

    let completionItem (declarationItem: FSharpDeclarationListItem) =
        let kind = kindString declarationItem.Glyph
        let filterText = filterText declarationItem
        let documentation = documentation declarationItem
        CompletionItem(declarationItem.Name, kind, filterText=filterText, documentation=documentation)

    let handleSubmitCode (codeSubmission: SubmitCode) (context: KernelInvocationContext) =
        async {
            let codeSubmissionReceived = CodeSubmissionReceived(codeSubmission)
            context.Publish(codeSubmissionReceived)
            resolvedAssemblies.Clear()
            let tokenSource = cancellationTokenSource
            let result, errors =
                try
                    script.Value.Eval(codeSubmission.Code, tokenSource.Token)
                with
                | ex -> Error(ex), [||]

            match result with
            | Ok(result) ->
                match result with
                | Some(value) ->
                    let value = value.ReflectionValue
                    let formattedValues = FormattedValue.FromObject(value)
                    context.Publish(ReturnValueProduced(value, codeSubmission, formattedValues))
                | None -> ()
            | Error(ex) ->
                if not (tokenSource.IsCancellationRequested) then
                    let aggregateError = String.Join("\n", errors)
                    let reportedException =
                        match ex with
                        | :? FsiCompilationException -> CodeSubmissionCompilationErrorException(ex) :> Exception
                        | _ -> ex
                    context.Fail(reportedException, aggregateError)
                else
                    context.Fail(null, "Command cancelled")
        }

    let handleRequestCompletion (requestCompletion: RequestCompletion) (context: KernelInvocationContext) =
        async {
            context.Publish(CompletionRequestReceived(requestCompletion))
            let l, c = getLineAndColumn requestCompletion.Code requestCompletion.CursorPosition
            let! declarationItems = script.Value.GetCompletionItems(requestCompletion.Code, l, c)
            let completionItems =
                declarationItems
                |> Array.map completionItem
            context.Publish(CompletionRequestCompleted(completionItems, requestCompletion))
        }

    let mutable dependencies: DependencyProvider option = None

    let getIdm(reportError: ResolvingErrorReport) : IDependencyManagerProvider =
        let idm = new Lazy<IDependencyManagerProvider>((fun () ->
            match dependencies with
            | None -> raise (new InvalidOperationException("Internal error --- must invoke ISupportNuget.InitializeDependencyProvider before ISupportNuget.Resolve()"))
            | Some deps -> deps.TryFindDependencyManagerByKey(Enumerable.Empty<string>(), "", reportError, "nuget")))
        idm.Force()

    member _.GetCurrentVariable(variableName: string) =
        let result, _errors =
            try
                script.Value.Eval("``" + variableName + "``")
            with
            | ex -> Error(ex), [||]
        match result with
        | Ok(Some(value)) -> Some (CurrentVariable(variableName, value.ReflectionType, value.ReflectionValue))
        | _ -> None

    member this.GetCurrentVariables() =
        // `ValueBound` event will make a copy of value types, so to ensure we always get the current value, we re-evaluate each variable
        variables
        |> Seq.filter (fun v -> v <> "it") // don't report special variable `it`
        |> Seq.choose (this.GetCurrentVariable)

    override _.HandleSubmitCode(command: SubmitCode, context: KernelInvocationContext): Task =
        handleSubmitCode command context |> Async.StartAsTask :> Task
        
    override _.HandleRequestCompletion(command: RequestCompletion, context: KernelInvocationContext): Task =
        handleRequestCompletion command context |> Async.StartAsTask :> Task

    override this.TryGetVariable(name: string, [<Out>] value: Object byref) =
        match this.GetCurrentVariable(name) with
        | Some(cv) ->
            value <- cv.Value
            true
        | None ->
            false

    interface ISupportNuget with

        member this.InitializeDependencyProvider(assemblyProbingPaths, nativeProbingRoots) =
            dependencies <- Some (new DependencyProvider(assemblyProbingPaths, nativeProbingRoots))

        member this.RegisterNugetResolvedPackageReferences (packageReferences: IReadOnlyList<ResolvedPackageReference>) =
            // Generate #r and #I from packageReferences
            let sb = StringBuilder()
            let hashset = HashSet()

            for reference in packageReferences do
                for assembly in reference.AssemblyPaths do
                    if hashset.Add(assembly.FullName) then
                        if assembly.Exists then
                            sb.AppendFormat("#r @\"{0}\"", assembly.FullName) |> ignore
                            sb.Append(Environment.NewLine) |> ignore

                match reference.PackageRoot with
                | null -> ()
                | root ->
                    if hashset.Add(root.FullName) then
                        if root.Exists then
                            sb.AppendFormat("#I @\"{0}\"", root.FullName) |> ignore
                            sb.Append(Environment.NewLine) |> ignore
            let command = new SubmitCode(sb.ToString(), "fsharp")
            this.DeferCommand(command)

        member this.Resolve(packageManagerTextLines:IEnumerable<string>, executionTfm: string, reportError: ResolvingErrorReport): IResolveDependenciesResult =
            //     Resolve reference for a list of package manager lines
            match dependencies with
            | None -> raise (new InvalidOperationException("Internal error --- must invoke ISupportNuget.InitializeDependencyProvider before ISupportNuget.Resolve()"))
            | Some deps -> deps.Resolve(getIdm(reportError), ".fsx", packageManagerTextLines, reportError, executionTfm)
