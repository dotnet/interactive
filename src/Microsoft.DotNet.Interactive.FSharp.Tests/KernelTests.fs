// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.DotNet.Interactive.FSharp.Tests

open System.Collections.Generic

open FluentAssertions
open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.FSharp
open Microsoft.DotNet.Interactive.Commands
open Microsoft.DotNet.Interactive.Events
open Xunit

type KernelTests() =
    let withKernel (action : Kernel -> (unit -> seq<KernelEvent>) -> 'a) =
        use k = new FSharpKernel()
        let mutable all = List<KernelEvent>()
        k.KernelEvents.Subscribe(fun e -> all.Add e) |> ignore

        let getEvents() =
            let evts = all
            all <- List()
            evts :> seq<_>

        action k getEvents

    let getHoverTexts (line : int) (column : int) (code : #seq<string>)  =
        let code = String.concat "\n" code
        withKernel <| fun kernel events ->
            let cmd =
                RequestHoverText(
                    code,
                    LinePosition(line, column),
                    kernel.Name
                )
            kernel.SendAsync(cmd).Wait()

            let texts = 
                events() 
                |> Seq.collect (function :? HoverTextProduced as e -> e.Content |> Seq.map (fun v -> v.Value) | _ -> Seq.empty)
                |> Seq.toArray
                |> String.concat "\n"
            
            texts

    let getCompletions (line : int) (column : int) (code : #seq<string>)  =
        let code = String.concat "\n" code
        withKernel <| fun kernel events ->
            let cmd =
                RequestCompletions(
                    code,
                    LinePosition(line, column),
                    kernel.Name
                )
            kernel.SendAsync(cmd).Wait()

            let completions = 
                events() 
                |> Seq.collect (function :? CompletionsProduced as e -> e.Completions |> Seq.map (fun v -> v) | _ -> Seq.empty)
                |> Seq.toArray
            
            completions

    [<Fact>]
    member __.``HoverText for Values``() =
        let texts =
            getHoverTexts 0 4 [
                "let a = 10"
            ]

        /// val a: int
        texts.Should().Contain(@"val a: int", null)
        
    [<Fact>]
    member __.``HoverText for Keywords``() =
        let texts =
            getHoverTexts 0 1 [
                "for i in 1 .. 10 do printfn \"foo %d\" i"
            ]

        /// for
        texts.Should().Contain(@"for", null)
        
    [<Fact>]
    member __.``HoverText for Methods``() =
        let texts =
            getHoverTexts 1 14 [ 
                "open System"
                "let a = Math.Sin(10.0)"
            ]

        // Math.Sin(a: float) : float
        texts.Should().ContainAll(@"static member Sin", "a: float", "-> float")

    [<Fact>]
    member __.``HoverText for Types``() =
        let texts =
            getHoverTexts 1 10 [ 
                "open System"
                "let a = Math.Sin(10.0)"
            ]

        // type Math =
        //     static val E : float
        //     ...
        texts.Should().Contain(@"type Math", null)
        
    [<Fact>]
    member __.``HoverText for Hidden Bindings``() =
        let texts =
            getHoverTexts 2 8 [ 
                "do"
                "    let a = 10"
                "    let a = 20.0"
                "    printfn \"%A\" a"
            ]

        // val a: float
        texts.Should().Contain(@"val a: float", null)
        
        
    [<Fact>]
    member __.``HoverText for Functions``() =
        let texts =
            getHoverTexts 0 9 [ 
                "let a = int 20.0"
            ]

        // val inline int : value:'T -> int (requires member op_Explicit)
        texts.Should().ContainAll("val inline int:", "^T (requires static member op_Explicit )", "-> int")

    [<Fact>]
    member __.``Get completion list for List module then get the 'average' function and verify it has documentation``() =
        let completionItem =
            getCompletions 0 5 [ 
                "List."
            ]
            |> Array.find (fun x -> x.DisplayText = "average")

        completionItem.Documentation.Should().Be("Returns the average of the values in a non-empty list.", null)
        
    [<Fact>]
    member _.``Completion items that need double backticks have the right display in tools but insert with the backticks``() =
        let src = """
type C() =
    member _.``Yee Yee`` = 12
C().
        """
        let completions = getCompletions 3 4 [src] |> Array.map (fun item -> item.DisplayText, item.InsertText)

        let hasYeeYeeBackticked = completions |> Array.contains ("Yee Yee", "``Yee Yee``")
        Assert.True(hasYeeYeeBackticked, $"Insert and display text aren't correct:\n%A{completions}")
