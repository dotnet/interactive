// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Runtime.InteropServices
open System.Text
open System.Threading
open System.Threading.Tasks

open Microsoft.CodeAnalysis.Tags
open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.Commands
open Microsoft.DotNet.Interactive.Events
open Microsoft.DotNet.Interactive.Extensions
open Microsoft.DotNet.Interactive.Utility

open Microsoft.DotNet.DependencyManager;
open FSharp.Compiler.Interactive.Shell
open FSharp.Compiler.Scripting
open FSharp.Compiler.SourceCodeServices

type FSharpKernel() as this =
    inherit DotNetLanguageKernel("fsharp")

    static let lockObj = Object();

    let variables = HashSet<string>()

    let createScript () =  
        lock lockObj (fun () -> new FSharpScript(additionalArgs=[|"/langversion:preview"|]))

    let extensionLoader: AssemblyBasedExtensionLoader = AssemblyBasedExtensionLoader()

    let script = lazy createScript ()
    do base.RegisterForDisposal(fun () -> if script.IsValueCreated then (script.Value :> IDisposable).Dispose())
    let mutable cancellationTokenSource = new CancellationTokenSource()

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
            let tokenSource = cancellationTokenSource
            let result, errors =
                try
                    script.Value.Eval(codeSubmission.Code, tokenSource.Token)
                with
                | ex -> Error(ex), [||]

            match result with
            | Ok(result) ->
                match result with
                | Some(value) when value.ReflectionType <> typeof<unit>  ->
                    let value = value.ReflectionValue
                    let formattedValues = FormattedValue.FromObject(value)
                    context.Publish(ReturnValueProduced(value, codeSubmission, formattedValues))
                | Some(value) -> ()
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
            let! declarationItems = script.Value.GetCompletionItems(requestCompletion.Code, requestCompletion.Position.Line + 1, requestCompletion.Position.Character)
            let completionItems =
                declarationItems
                |> Array.map completionItem
            context.Publish(CompletionRequestCompleted(completionItems, requestCompletion))
        }

    let createPackageRestoreContext registerForDisposal =
        let packageRestoreContext = new PackageRestoreContext()
        do registerForDisposal(fun () -> packageRestoreContext.Dispose())
        packageRestoreContext

    let _packageRestoreContext = lazy createPackageRestoreContext this.RegisterForDisposal

            if isNull _nativeProbingRoots then
                raise (new InvalidOperationException("_nativeProbingRoots is null"))
            new DependencyProvider(_assemblyProbingPaths, _nativeProbingRoots)
        lazy (createDependencyProvider ())

    member this.GetCurrentVariables() =
        script.Value.Fsi.GetBoundValues()
        |> List.filter (fun x -> x.Name <> "it") // don't report special variable `it`
        |> List.map (fun x -> CurrentVariable(x.Name, x.Value.ReflectionType, x.Value.ReflectionValue))

    override _.HandleSubmitCode(command: SubmitCode, context: KernelInvocationContext): Task =
        handleSubmitCode command context |> Async.StartAsTask :> Task
        
    override _.HandleRequestCompletion(command: RequestCompletion, context: KernelInvocationContext): Task =
        handleRequestCompletion command context |> Async.StartAsTask :> Task

    override _.TryGetVariable<'a>(name: string, [<Out>] value: 'a byref) =
        match script.Value.Fsi.TryFindBoundValue(name) with
        | Some cv ->
            value <- cv.Value.ReflectionValue :?> 'a
            true
        | _ ->
            false

    override _.SetVariableAsync(name: string, value: Object) : Task = 
        // script.Value.Fsi.AddBoundValue(name, value) |> ignore
        Task.CompletedTask

    member _.RestoreSources = _packageRestoreContext.Value.RestoreSources;

    member _.RequestedPackageReferences = _packageRestoreContext.Value.RequestedPackageReferences;

    member _.ResolvedPackageReferences = _packageRestoreContext.Value.ResolvedPackageReferences;

    member _.PackageRestoreContext = _packageRestoreContext.Value

    // Integrate nuget package management to the F# Kernel
    interface ISupportNuget with
        member _.RegisterResolvedPackageReferences (packageReferences: IReadOnlyList<ResolvedPackageReference>) =
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

        member _.PackageRestoreContext = this.PackageRestoreContext

    interface IExtensibleKernel with
        member this.LoadExtensionsFromDirectoryAsync(directory:DirectoryInfo, context:KernelInvocationContext) =
            extensionLoader.LoadFromDirectoryAsync(directory, this, context)
