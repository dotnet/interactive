namespace Microsoft.DotNet.Interactive.FSharp

open System
open System.IO
open FSharp.Reflection
open Microsoft.DotNet.Interactive.Formatting
open Internal.Utilities.StructuredFormat // from FSharp.Compiler
open FSharp.Compiler.Layout // from FSharp.Compiler

type IAnyToLayoutCall = 
    abstract AnyToLayout : FormatOptions * obj * Type -> Internal.Utilities.StructuredFormat.Layout
    abstract FsiAnyToLayout : FormatOptions * obj * Type -> Internal.Utilities.StructuredFormat.Layout

type private AnyToLayoutSpecialization<'T>() = 
    interface IAnyToLayoutCall with
        member this.AnyToLayout(options, o : obj, ty : Type) = Internal.Utilities.StructuredFormat.Display.any_to_layout options ((Unchecked.unbox o : 'T), ty)
        member this.FsiAnyToLayout(options, o : obj, ty : Type) = Internal.Utilities.StructuredFormat.Display.fsi_any_to_layout options ((Unchecked.unbox o : 'T), ty)

module internal FSharpPlainText = 
    let getAnyToLayoutCall ty = 
        let specialized = typedefof<AnyToLayoutSpecialization<_>>.MakeGenericType [| ty |]
        Activator.CreateInstance(specialized) :?> IAnyToLayoutCall
    
    let formatObject (object: obj) (writer: TextWriter) =
            let options = 
              { FormatOptions.Default with 
                    // TODO get these settings from somewhere
                    FormatProvider = fsi.FormatProvider
                    FloatingPointFormat = fsi.FloatingPointFormat
                    ShowProperties = fsi.ShowProperties
                    ShowIEnumerable = fsi.ShowIEnumerable 
                    PrintIntercepts = 

                      let printers : Choice<(System.Type * (obj -> string)), (System.Type * (obj -> obj))> list = []
                          //fsi.GetType().GetProperty("AddedPrinters",BindingFlags.Instance ||| BindingFlags.Public ||| BindingFlags.NonPublic).GetValue(fsi)
                          //|> unbox

                      [ for x in printers do 
                           match x with 
                           | Choice1Of2 (aty: System.Type, printer) -> 
                                  yield (fun _ienv (obj:obj) ->
                                     match obj with 
                                     | null -> None 
                                     | _ when aty.IsAssignableFrom(obj.GetType())  ->  
                                         match printer obj with 
                                         | null -> None
                                         | s -> Some (wordL (TaggedTextOps.tagText s)) 
                                     | _ -> None)
                                   
                           | Choice2Of2 (aty: System.Type, converter) -> 
                                  yield (fun ienv (obj:obj) ->
                                     match obj with 
                                     | null -> None 
                                     | _ when aty.IsAssignableFrom(obj.GetType())  -> 
                                         match converter obj with 
                                         | null -> None
                                         | res -> Some (ienv.GetLayout res)
                                     | _ -> None) ]

                    PrintWidth = fsi.PrintWidth 
                    PrintDepth = Formatter.RecursionLimit
                    PrintLength = Formatter.ListExpansionLimit
                    PrintSize = fsi.PrintSize
                }

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
            
