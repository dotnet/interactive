
namespace Microsoft.DotNet.Interactive.FSharp.Tests

open Microsoft.DotNet.Interactive
open Microsoft.DotNet.Interactive.FSharp
open Microsoft.DotNet.Interactive.FSharp.FSharpKernelHelpers.Html
open Xunit
open Microsoft.DotNet.Interactive.Commands
open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Text
open Microsoft.DotNet.Interactive.Events
open System.Collections.Generic

[<AutoOpen>]
module AssertExtensions =   
    open System.Text.RegularExpressions
    
    let wsRx = Regex @"([a-z_A-Z0-9][ ]+[a-z_A-Z0-9])|([^a-z_A-Z0-9][ ]+[a-z_A-Z0-9])|([a-z_A-Z0-9][ ]+[^a-z_A-Z0-9])|([^a-z_A-Z0-9][ ]+[^a-z_A-Z0-9])"
    type Assert with

        static member ContainsSubstring(expected : string, actual : seq<string>) =
            if not (actual |> Seq.exists (fun a -> a.Contains expected)) then
                raise <| Xunit.Sdk.ContainsException(expected, actual)

        static member ContainsMatching(pattern : string, actual : seq<string>) =

            let expectedPattern = 
                wsRx.Replace(pattern, fun m ->  
                    if m.Groups.[1].Success then m.Value.Substring(0, 1) + "[ \t\r\n]+" + m.Value.Substring(2, 1) 
                    else m.Value.Substring(0, 1) + "[ \t\r\n]*" + m.Value.Substring(2, 1)
                )



            let rx = Regex expectedPattern
            if not (actual |> Seq.exists (fun a -> rx.IsMatch a)) then
                raise <| Xunit.Sdk.ContainsException(pattern, actual)


type KernelTests() =
    static let withKernel (action : Kernel -> (unit -> seq<KernelEvent>) -> 'a) =
        use k = new FSharpKernel()
        let mutable all = List<KernelEvent>()
        use __ = k.KernelEvents.Subscribe(fun e -> lock all (fun () -> all.Add e))

        let getEvents() =
            let evts = all
            all <- List()
            evts :> seq<_>

        action k getEvents

    static let getHoverTexts (line : int) (column : int) (code : #seq<string>)  =
        let code = String.concat "\r\n" code
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
            
            texts


    [<Fact>]
    member __.``HoverText for Values``() =
        let texts =
            getHoverTexts 0 4[ 
                "let a = 10"
            ]

        /// val a : int
        Assert.ContainsMatching(@"val a : int", texts)
        
    [<Fact>]
    member __.``HoverText for Keywords``() =
        let texts =
            getHoverTexts 0 1 [
                "for i in 1 .. 10 do printfn \"foo %d\" i"
            ]

        /// for
        Assert.ContainsMatching(@"for", texts)
        
    [<Fact>]
    member __.``HoverText for Methods``() =
        let texts =
            getHoverTexts 1 14 [ 
                "open System"
                "let a = Math.Sin(10.0)"
            ]

        // Math.Sin(a: float) : float
        Assert.ContainsMatching(@"static member Sin: \w+: float \-\> float", texts)

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
        Assert.ContainsMatching(@"type Math", texts)
        
    [<Fact>]
    member __.``HoverText for Hidden Bindings``() =
        let texts =
            getHoverTexts 2 8 [ 
                "do"
                "    let a = 10"
                "    let a = 20.0"
                "    printfn \"%A\" a"
            ]

        // val a : float
        Assert.ContainsMatching(@"val a : float", texts)
        
        
    [<Fact>]
    member __.``HoverText for Functions``() =
        let texts =
            getHoverTexts 0 9 [ 
                "let a = int 20.0"
            ]

        // val int : value:'T -> int (requires member op_Explicit)
        Assert.ContainsMatching(@"val int: \w+: \^\w+ \( requires static member op_Explicit \) \-\> int", texts)
        


