// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp

open System
open System.Collections.Generic
open System.Runtime.InteropServices
open System.Threading
open System.Threading.Tasks

open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.Commands
open Microsoft.DotNet.Interactive.Events
open Microsoft.DotNet.Interactive.Utility

open FSharp.Compiler.Interactive.Shell
open FSharp.Compiler.Scripting
open FSharp.DependencyManager
open FSharp.Compiler.SourceCodeServices
open Microsoft.CodeAnalysis.Tags
open System

type FSharpKernel() as this =
    inherit KernelBase(Name = "fsharp")

    let resolvedAssemblies = List<string>()
    static let lockObj = Object();

    let variables = HashSet<string>()

    let createScript registerForDisposal  =  
        let script = lock lockObj (fun () -> new FSharpScript(additionalArgs=[|"/langversion:preview"|]))

        let valueBoundHandler = new Handler<(obj * Type * string)>(fun _ (_, _, name) -> variables.Add(name) |> ignore)
        do script.ValueBound.AddHandler valueBoundHandler
        do registerForDisposal(fun () -> script.ValueBound.RemoveHandler valueBoundHandler)

        let handler = new Handler<string> (fun o s -> resolvedAssemblies.Add(s))
        do script.AssemblyReferenceAdded.AddHandler handler
        do registerForDisposal(fun () -> do script.AssemblyReferenceAdded.RemoveHandler handler)
        script

    let script = lazy createScript this.RegisterForDisposal
    do base.RegisterForDisposal(fun () -> if script.IsValueCreated then (script.Value :> IDisposable).Dispose())
    let mutable cancellationTokenSource = new CancellationTokenSource()

    let messageMap = Dictionary<string, string>()

    let parseReference text =
        let reference, binLogPath = FSharpDependencyManager.parsePackageReference [text]
        (reference |> List.tryHead), binLogPath

    let packageInstallingMessages (refSpec: PackageReference option * string option option) =
        let ref, binLogPath = refSpec
        let versionText =
            match ref with
            | Some ref when ref.Version <> "*" -> ", version " + ref.Version
            | _ -> ""

        let installingMessage ref = "Installing package " + ref.Include + versionText + "."
        let loggingMessage = "Binary Logging enabled"
        [|
            match ref, binLogPath with
            | Some reference, Some _ ->
                yield installingMessage reference
                yield loggingMessage
            | Some reference, None ->
                yield installingMessage reference
            | None, Some _ ->
                yield loggingMessage
            | None, None ->
                ()
        |]

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
            use _ = script.Value.DependencyAdding
                    |> Observable.subscribe (fun (key, referenceText) ->
                        if key = "nuget" then
                            let reference = parseReference referenceText
                            for message in packageInstallingMessages reference do
                                let key = message
                                messageMap.[key] <- message
                                context.Publish(DisplayedValueProduced(message, context.Command, valueId=key))
                        ())

            use _ = script.Value.DependencyAdded
                    |> Observable.subscribe (fun (key, referenceText) ->
                        if key = "nuget" then
                            let reference = parseReference referenceText
                            match reference with
                            | Some ref, _ ->
                                let packageRef = ResolvedPackageReference(ref.Include, packageVersion=ref.Version, assemblyPaths=[])
                                context.Publish(PackageAdded(packageRef))
                            | _ -> ()

                            for key in packageInstallingMessages reference do
                                match reference with
                                | Some ref, _ ->
                                    let packageRef = ResolvedPackageReference(ref.Include, packageVersion=ref.Version, assemblyPaths=[])
                                    let message = "Installed package " + packageRef.PackageName + " version " + packageRef.PackageVersion
                                    context.Publish(DisplayedValueUpdated(message, key))
                                | _ -> ()
                            ())

            use _ = script.Value.DependencyFailed
                    |> Observable.subscribe (fun (key, referenceText) ->
                        if key = "nuget" then
                            let reference = parseReference referenceText
                            for key in packageInstallingMessages reference do
                                let message = messageMap.[key] + "failed!"
                                context.Publish(DisplayedValueUpdated(message, key))
                            ())

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
