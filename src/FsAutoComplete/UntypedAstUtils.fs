/// Code from VisualFSharpPowerTools project: https://github.com/fsprojects/VisualFSharpPowerTools/blob/master/src/FSharp.Editing/Common/UntypedAstUtils.fs
module FsAutoComplete.UntypedAstUtils

//open FSharp.Compiler.SyntaxTree
//open System.Collections.Generic
//open FSharp.Compiler
//open FSharp.Compiler.Range

//type Range.range with
//    member inline x.IsEmpty = x.StartColumn = x.EndColumn && x.StartLine = x.EndLine

//type internal ShortIdent = string
//type internal Idents = ShortIdent[]

//let internal longIdentToArray (longIdent: LongIdent): Idents =
//    longIdent |> Seq.map string |> Seq.toArray

///// An recursive pattern that collect all sequential expressions to avoid StackOverflowException
//let rec (|Sequentials|_|) = function
//    | SynExpr.Sequential(_, _, e, Sequentials es, _) ->
//        Some(e::es)
//    | SynExpr.Sequential(_, _, e1, e2, _) ->
//        Some [e1; e2]
//    | _ -> None

//let (|ConstructorPats|) = function
//    | SynArgPats.Pats ps -> ps
//    | SynArgPats.NamePatPairs(xs, _) -> List.map snd xs

///// A pattern that collects all attributes from a `SynAttributes` into a single flat list
//let (|AllAttrs|) (attrs: SynAttributes) =
//    attrs |> List.collect (fun attrList -> attrList.Attributes)

///// A pattern that collects all patterns from a `SynSimplePats` into a single flat list
//let (|AllSimplePats|) (pats: SynSimplePats) =
//    let rec loop acc pat =
//        match pat with
//        | SynSimplePats.SimplePats (pats,_) -> acc @ pats
//        | SynSimplePats.Typed(pats,_,_) -> loop acc pats

//    loop [] pats

///// Returns all Idents and LongIdents found in an untyped AST.
//let internal getLongIdents (input: ParsedInput option) : IDictionary<Range.pos, Idents> =
//    let identsByEndPos = Dictionary<Range.pos, Idents>()

//    let addLongIdent (longIdent: LongIdent) =
//        let idents = longIdentToArray longIdent
//        for ident in longIdent do
//            identsByEndPos.[ident.idRange.End] <- idents

//    let addLongIdentWithDots (LongIdentWithDots (longIdent, lids) as value) =
//        match longIdentToArray longIdent with
//        | [||] -> ()
//        | [|_|] as idents -> identsByEndPos.[value.Range.End] <- idents
//        | idents ->
//            for dotRange in lids do
//                identsByEndPos.[Range.mkPos dotRange.EndLine (dotRange.EndColumn - 1)] <- idents
//            identsByEndPos.[value.Range.End] <- idents

//    let addIdent (ident: Ident) =
//        identsByEndPos.[ident.idRange.End] <- [|ident.idText|]

//    let rec walkImplFileInput (ParsedImplFileInput(_, _, _, _, _, moduleOrNamespaceList, _)) =
//        List.iter walkSynModuleOrNamespace moduleOrNamespaceList

//    and walkSynModuleOrNamespace (SynModuleOrNamespace(_, _, _, decls, _, AllAttrs attrs, _, _)) =
//        List.iter walkAttribute attrs
//        List.iter walkSynModuleDecl decls

//    and walkAttribute (attr: SynAttribute) =
//        addLongIdentWithDots attr.TypeName
//        walkExpr attr.ArgExpr

//    and walkTyparDecl (SynTyparDecl.TyparDecl (AllAttrs attrs, typar)) =
//        List.iter walkAttribute attrs
//        walkTypar typar

//    and walkTypeConstraint = function
//        | SynTypeConstraint.WhereTyparIsValueType (t, _)
//        | SynTypeConstraint.WhereTyparIsReferenceType (t, _)
//        | SynTypeConstraint.WhereTyparIsUnmanaged (t, _)
//        | SynTypeConstraint.WhereTyparSupportsNull (t, _)
//        | SynTypeConstraint.WhereTyparIsComparable (t, _)
//        | SynTypeConstraint.WhereTyparIsEquatable (t, _) -> walkTypar t
//        | SynTypeConstraint.WhereTyparDefaultsToType (t, ty, _)
//        | SynTypeConstraint.WhereTyparSubtypeOfType (t, ty, _) -> walkTypar t; walkType ty
//        | SynTypeConstraint.WhereTyparIsEnum (t, ts, _)
//        | SynTypeConstraint.WhereTyparIsDelegate (t, ts, _) -> walkTypar t; List.iter walkType ts
//        | SynTypeConstraint.WhereTyparSupportsMember (ts, sign, _) -> List.iter walkType ts; walkMemberSig sign

//    and walkPat = function
//        | SynPat.Tuple (_, pats, _)
//        | SynPat.ArrayOrList (_, pats, _)
//        | SynPat.Ands (pats, _) -> List.iter walkPat pats
//        | SynPat.Named (pat, ident, _, _, _) ->
//            walkPat pat
//            addIdent ident
//        | SynPat.Typed (pat, t, _) ->
//            walkPat pat
//            walkType t
//        | SynPat.Attrib (pat, AllAttrs attrs, _) ->
//            walkPat pat
//            List.iter walkAttribute attrs
//        | SynPat.Or (pat1, pat2, _) -> List.iter walkPat [pat1; pat2]
//        | SynPat.LongIdent (ident, _, typars, ConstructorPats pats, _, _) ->
//            addLongIdentWithDots ident
//            typars
//            |> Option.iter (fun (SynValTyparDecls (typars, _, constraints)) ->
//                 List.iter walkTyparDecl typars
//                 List.iter walkTypeConstraint constraints)
//            List.iter walkPat pats
//        | SynPat.Paren (pat, _) -> walkPat pat
//        | SynPat.IsInst (t, _) -> walkType t
//        | SynPat.QuoteExpr(e, _) -> walkExpr e
//        | _ -> ()

//    and walkTypar (Typar (_, _, _)) = ()

//    and walkBinding (SynBinding.Binding (_, _, _, _, AllAttrs attrs, _, _, pat, returnInfo, e, _, _)) =
//        List.iter walkAttribute attrs
//        walkPat pat
//        walkExpr e
//        returnInfo |> Option.iter (fun (SynBindingReturnInfo (t, _, _)) -> walkType t)

//    and walkInterfaceImpl (InterfaceImpl(_, bindings, _)) = List.iter walkBinding bindings

//    and walkIndexerArg = function
//        | SynIndexerArg.One(e, _fromEnd,_range) -> walkExpr e
//        | SynIndexerArg.Two(e1,_e1FromEnd,e2,_e2FromEnd,_e1Range,_e2Range) -> List.iter walkExpr [e1; e2]

//    and walkType = function
//        | SynType.Array (_, t, _)
//        | SynType.HashConstraint (t, _)
//        | SynType.MeasurePower (t, _, _) -> walkType t
//        | SynType.Fun (t1, t2, _)
//        | SynType.MeasureDivide (t1, t2, _) -> walkType t1; walkType t2
//        | SynType.LongIdent ident -> addLongIdentWithDots ident
//        | SynType.App (ty, _, types, _, _, _, _) -> walkType ty; List.iter walkType types
//        | SynType.LongIdentApp (_, _, _, types, _, _, _) -> List.iter walkType types
//        | SynType.Tuple (_, ts, _) -> ts |> List.iter (fun (_, t) -> walkType t)
//        | SynType.WithGlobalConstraints (t, typeConstraints, _) ->
//            walkType t; List.iter walkTypeConstraint typeConstraints
//        | _ -> ()

//    and walkClause (Clause (pat, e1, e2, _, _)) =
//        walkPat pat
//        walkExpr e2
//        e1 |> Option.iter walkExpr

//    and walkSimplePats = function
//        | SynSimplePats.SimplePats (pats, _) -> List.iter walkSimplePat pats
//        | SynSimplePats.Typed (pats, ty, _) ->
//            walkSimplePats pats
//            walkType ty

//    and walkExpr = function
//        | SynExpr.Paren (e, _, _, _)
//        | SynExpr.Quote (_, _, e, _, _)
//        | SynExpr.Typed (e, _, _)
//        | SynExpr.InferredUpcast (e, _)
//        | SynExpr.InferredDowncast (e, _)
//        | SynExpr.AddressOf (_, e, _, _)
//        | SynExpr.DoBang (e, _)
//        | SynExpr.YieldOrReturn (_, e, _)
//        | SynExpr.ArrayOrListOfSeqExpr (_, e, _)
//        | SynExpr.CompExpr (_, _, e, _)
//        | SynExpr.Do (e, _)
//        | SynExpr.Assert (e, _)
//        | SynExpr.Lazy (e, _)
//        | SynExpr.YieldOrReturnFrom (_, e, _) -> walkExpr e
//        | SynExpr.Lambda (_, _, pats, e, _) ->
//            walkSimplePats pats
//            walkExpr e
//        | SynExpr.New (_, t, e, _)
//        | SynExpr.TypeTest (e, t, _)
//        | SynExpr.Upcast (e, t, _)
//        | SynExpr.Downcast (e, t, _) -> walkExpr e; walkType t
//        | SynExpr.Tuple (_, es, _, _)
//        | Sequentials es
//        | SynExpr.ArrayOrList (_, es, _) -> List.iter walkExpr es
//        | SynExpr.App (_, _, e1, e2, _)
//        | SynExpr.TryFinally (e1, e2, _, _, _)
//        | SynExpr.While (_, e1, e2, _) -> List.iter walkExpr [e1; e2]
//        | SynExpr.Record (_, _, fields, _) ->
//            fields |> List.iter (fun ((ident, _), e, _) ->
//                        addLongIdentWithDots ident
//                        e |> Option.iter walkExpr)
//        | SynExpr.Ident ident -> addIdent ident
//        | SynExpr.ObjExpr(ty, argOpt, bindings, ifaces, _, _) ->
//            argOpt |> Option.iter (fun (e, ident) ->
//                walkExpr e
//                ident |> Option.iter addIdent)
//            walkType ty
//            List.iter walkBinding bindings
//            List.iter walkInterfaceImpl ifaces
//        | SynExpr.LongIdent (_, ident, _, _) -> addLongIdentWithDots ident
//        | SynExpr.For (_, ident, e1, _, e2, e3, _) ->
//            addIdent ident
//            List.iter walkExpr [e1; e2; e3]
//        | SynExpr.ForEach (_, _, _, pat, e1, e2, _) ->
//            walkPat pat
//            List.iter walkExpr [e1; e2]
//        | SynExpr.MatchLambda (_, _, synMatchClauseList, _, _) ->
//            List.iter walkClause synMatchClauseList
//        | SynExpr.Match (_, e, synMatchClauseList, _) ->
//            walkExpr e
//            List.iter walkClause synMatchClauseList
//        | SynExpr.TypeApp (e, _, tys, _, _, _, _) ->
//            List.iter walkType tys; walkExpr e
//        | SynExpr.LetOrUse (_, _, bindings, e, _) ->
//            List.iter walkBinding bindings; walkExpr e
//        | SynExpr.TryWith (e, _, clauses, _, _, _, _) ->
//            List.iter walkClause clauses;  walkExpr e
//        | SynExpr.IfThenElse (e1, e2, e3, _, _, _, _) ->
//            List.iter walkExpr [e1; e2]
//            e3 |> Option.iter walkExpr
//        | SynExpr.LongIdentSet (ident, e, _)
//        | SynExpr.DotGet (e, _, ident, _) ->
//            addLongIdentWithDots ident
//            walkExpr e
//        | SynExpr.DotSet (e1, idents, e2, _) ->
//            walkExpr e1
//            addLongIdentWithDots idents
//            walkExpr e2
//        | SynExpr.DotIndexedGet (e, args, _, _) ->
//            walkExpr e
//            List.iter walkIndexerArg args
//        | SynExpr.DotIndexedSet (e1, args, e2, _, _, _) ->
//            walkExpr e1
//            List.iter walkIndexerArg args
//            walkExpr e2
//        | SynExpr.NamedIndexedPropertySet (ident, e1, e2, _) ->
//            addLongIdentWithDots ident
//            List.iter walkExpr [e1; e2]
//        | SynExpr.DotNamedIndexedPropertySet (e1, ident, e2, e3, _) ->
//            addLongIdentWithDots ident
//            List.iter walkExpr [e1; e2; e3]
//        | SynExpr.JoinIn (e1, _, e2, _) -> List.iter walkExpr [e1; e2]
//        | SynExpr.LetOrUseBang (_, _, _, pat, e1, ands, e2, _) ->
//            walkPat pat
//            walkExpr e1
//            for (_,_,_,pat,body,_) in ands do
//              walkPat pat
//              walkExpr body
//            walkExpr e2
//        | SynExpr.TraitCall (ts, sign, e, _) ->
//            List.iter walkTypar ts
//            walkMemberSig sign
//            walkExpr e
//        | SynExpr.Const (SynConst.Measure(_, m), _) -> walkMeasure m
//        | _ -> ()

//    and walkMeasure = function
//        | SynMeasure.Product (m1, m2, _)
//        | SynMeasure.Divide (m1, m2, _) -> walkMeasure m1; walkMeasure m2
//        | SynMeasure.Named (longIdent, _) -> addLongIdent longIdent
//        | SynMeasure.Seq (ms, _) -> List.iter walkMeasure ms
//        | SynMeasure.Power (m, _, _) -> walkMeasure m
//        | SynMeasure.Var (ty, _) -> walkTypar ty
//        | SynMeasure.One
//        | SynMeasure.Anon _ -> ()

//    and walkSimplePat = function
//        | SynSimplePat.Attrib (pat, AllAttrs attrs, _) ->
//            walkSimplePat pat
//            List.iter walkAttribute attrs
//        | SynSimplePat.Typed(pat, t, _) ->
//            walkSimplePat pat
//            walkType t
//        | _ -> ()

//    and walkField (SynField.Field(AllAttrs attrs, _, _, t, _, _, _, _)) =
//        List.iter walkAttribute attrs
//        walkType t

//    and walkValSig (SynValSig.ValSpfn(AllAttrs attrs, _, _, t, SynValInfo(argInfos, argInfo), _, _, _, _, _, _)) =
//        List.iter walkAttribute attrs
//        walkType t
//        argInfo :: (argInfos |> List.concat)
//        |> List.collect (fun (SynArgInfo(AllAttrs attrs, _, _)) -> attrs)
//        |> List.iter walkAttribute

//    and walkMemberSig = function
//        | SynMemberSig.Inherit (t, _)
//        | SynMemberSig.Interface(t, _) -> walkType t
//        | SynMemberSig.Member(vs, _, _) -> walkValSig vs
//        | SynMemberSig.ValField(f, _) -> walkField f
//        | SynMemberSig.NestedType(SynTypeDefnSig.TypeDefnSig (info, repr, memberSigs, _), _) ->
//            let isTypeExtensionOrAlias =
//                match repr with
//                | SynTypeDefnSigRepr.Simple(SynTypeDefnSimpleRepr.TypeAbbrev _, _)
//                | SynTypeDefnSigRepr.ObjectModel(SynTypeDefnKind.TyconAbbrev, _, _)
//                | SynTypeDefnSigRepr.ObjectModel(SynTypeDefnKind.TyconAugmentation, _, _) -> true
//                | _ -> false
//            walkComponentInfo isTypeExtensionOrAlias info
//            walkTypeDefnSigRepr repr
//            List.iter walkMemberSig memberSigs

//    and walkMember = function
//        | SynMemberDefn.AbstractSlot (valSig, _, _) -> walkValSig valSig
//        | SynMemberDefn.Member (binding, _) -> walkBinding binding
//        | SynMemberDefn.ImplicitCtor (_, AllAttrs attrs, AllSimplePats pats, _, _) ->
//            List.iter walkAttribute attrs
//            List.iter walkSimplePat pats
//        | SynMemberDefn.ImplicitInherit (t, e, _, _) -> walkType t; walkExpr e
//        | SynMemberDefn.LetBindings (bindings, _, _, _) -> List.iter walkBinding bindings
//        | SynMemberDefn.Interface (t, members, _) ->
//            walkType t
//            members |> Option.iter (List.iter walkMember)
//        | SynMemberDefn.Inherit (t, _, _) -> walkType t
//        | SynMemberDefn.ValField (field, _) -> walkField field
//        | SynMemberDefn.NestedType (tdef, _, _) -> walkTypeDefn tdef
//        | SynMemberDefn.AutoProperty (AllAttrs attrs, _, _, t, _, _, _, _, e, _, _) ->
//            List.iter walkAttribute attrs
//            Option.iter walkType t
//            walkExpr e
//        | _ -> ()

//    and walkEnumCase (EnumCase(AllAttrs attrs, _, _, _, _)) = List.iter walkAttribute attrs

//    and walkUnionCaseType = function
//        | SynUnionCaseType.UnionCaseFields fields -> List.iter walkField fields
//        | SynUnionCaseType.UnionCaseFullType (t, _) -> walkType t

//    and walkUnionCase (SynUnionCase.UnionCase (AllAttrs attrs, _, t, _, _, _)) =
//        List.iter walkAttribute attrs
//        walkUnionCaseType t

//    and walkTypeDefnSimple = function
//        | SynTypeDefnSimpleRepr.Enum (cases, _) -> List.iter walkEnumCase cases
//        | SynTypeDefnSimpleRepr.Union (_, cases, _) -> List.iter walkUnionCase cases
//        | SynTypeDefnSimpleRepr.Record (_, fields, _) -> List.iter walkField fields
//        | SynTypeDefnSimpleRepr.TypeAbbrev (_, t, _) -> walkType t
//        | _ -> ()

//    and walkComponentInfo isTypeExtensionOrAlias (ComponentInfo(AllAttrs attrs, typars, constraints, longIdent, _, _, _, _)) =
//        List.iter walkAttribute attrs
//        List.iter walkTyparDecl typars
//        List.iter walkTypeConstraint constraints
//        if isTypeExtensionOrAlias then
//            addLongIdent longIdent

//    and walkTypeDefnRepr = function
//        | SynTypeDefnRepr.ObjectModel (_, defns, _) -> List.iter walkMember defns
//        | SynTypeDefnRepr.Simple(defn, _) -> walkTypeDefnSimple defn
//        | SynTypeDefnRepr.Exception _ -> ()

//    and walkTypeDefnSigRepr = function
//        | SynTypeDefnSigRepr.ObjectModel (_, defns, _) -> List.iter walkMemberSig defns
//        | SynTypeDefnSigRepr.Simple(defn, _) -> walkTypeDefnSimple defn
//        | SynTypeDefnSigRepr.Exception _ -> ()

//    and walkTypeDefn (TypeDefn (info, repr, members, _)) =
//        let isTypeExtensionOrAlias =
//            match repr with
//            | SynTypeDefnRepr.ObjectModel (SynTypeDefnKind.TyconAugmentation, _, _)
//            | SynTypeDefnRepr.ObjectModel (SynTypeDefnKind.TyconAbbrev, _, _)
//            | SynTypeDefnRepr.Simple (SynTypeDefnSimpleRepr.TypeAbbrev _, _) -> true
//            | _ -> false
//        walkComponentInfo isTypeExtensionOrAlias info
//        walkTypeDefnRepr repr
//        List.iter walkMember members

//    and walkSynModuleDecl (decl: SynModuleDecl) =
//        match decl with
//        | SynModuleDecl.NamespaceFragment fragment -> walkSynModuleOrNamespace fragment
//        | SynModuleDecl.NestedModule (info, _, modules, _, _) ->
//            walkComponentInfo false info
//            List.iter walkSynModuleDecl modules
//        | SynModuleDecl.Let (_, bindings, _) -> List.iter walkBinding bindings
//        | SynModuleDecl.DoExpr (_, expr, _) -> walkExpr expr
//        | SynModuleDecl.Types (types, _) -> List.iter walkTypeDefn types
//        | SynModuleDecl.Attributes (AllAttrs attrs, _) -> List.iter walkAttribute attrs
//        | _ -> ()

//    match input with
//    | Some (ParsedInput.ImplFile input) ->
//         walkImplFileInput input
//    | _ -> ()
//    //debug "%A" idents
//    identsByEndPos :> _

///// Checks if given position is part of the typed binding
//let internal isTypedBindingAtPosition (input: ParsedInput option) (r: range) : bool =
//    let mutable result = false

//    let isInside (ran : range) =
//        Range.rangeContainsRange ran r

//    let rec walkImplFileInput (ParsedImplFileInput(_, _, _, _, _, moduleOrNamespaceList, _)) =
//        List.iter walkSynModuleOrNamespace moduleOrNamespaceList

//    and walkSynModuleOrNamespace (SynModuleOrNamespace(_, _, _, decls, _, AllAttrs attrs, _, _)) =
//        List.iter walkAttribute attrs
//        List.iter walkSynModuleDecl decls

//    and walkAttribute (attr: SynAttribute) =
//        walkExpr attr.ArgExpr

//    and walkTyparDecl (SynTyparDecl.TyparDecl (AllAttrs attrs, typar)) =
//        List.iter walkAttribute attrs
//        walkTypar typar

//    and walkTypeConstraint = function
//        | SynTypeConstraint.WhereTyparIsValueType (t, _)
//        | SynTypeConstraint.WhereTyparIsReferenceType (t, _)
//        | SynTypeConstraint.WhereTyparIsUnmanaged (t, _)
//        | SynTypeConstraint.WhereTyparSupportsNull (t, _)
//        | SynTypeConstraint.WhereTyparIsComparable (t, _)
//        | SynTypeConstraint.WhereTyparIsEquatable (t, _) -> walkTypar t
//        | SynTypeConstraint.WhereTyparDefaultsToType (t, ty, _)
//        | SynTypeConstraint.WhereTyparSubtypeOfType (t, ty, _) -> walkTypar t; walkType ty
//        | SynTypeConstraint.WhereTyparIsEnum (t, ts, _)
//        | SynTypeConstraint.WhereTyparIsDelegate (t, ts, _) -> walkTypar t; List.iter walkType ts
//        | SynTypeConstraint.WhereTyparSupportsMember (ts, sign, _) -> List.iter walkType ts; walkMemberSig sign

//    and walkPat = function
//        | SynPat.Tuple (_, pats, _)
//        | SynPat.ArrayOrList (_, pats, _)
//        | SynPat.Ands (pats, _) -> List.iter walkPat pats
//        | SynPat.Named (pat, ident, _, _, _) ->
//            walkPat pat
//        | SynPat.Typed (pat, t, ran) ->
//            if isInside ran then result <- true
//            walkPat pat
//            walkType t
//        | SynPat.Attrib (pat, AllAttrs attrs, _) ->
//            walkPat pat
//            List.iter walkAttribute attrs
//        | SynPat.Or (pat1, pat2, _) -> List.iter walkPat [pat1; pat2]
//        | SynPat.LongIdent (ident, _, typars, ConstructorPats pats, _, _) ->
//            typars
//            |> Option.iter (fun (SynValTyparDecls (typars, _, constraints)) ->
//                 List.iter walkTyparDecl typars
//                 List.iter walkTypeConstraint constraints)
//            List.iter walkPat pats
//        | SynPat.Paren (pat, _) -> walkPat pat
//        | SynPat.IsInst (t, _) -> walkType t
//        | SynPat.QuoteExpr(e, _) -> walkExpr e
//        | _ -> ()

//    and walkTypar (Typar (_, _, _)) = ()

//    and walkBinding (SynBinding.Binding (_, _, _, _, AllAttrs attrs, _, _, pat, returnInfo, e, _, _)) =
//        List.iter walkAttribute attrs
//        walkPat pat
//        walkExpr e
//        returnInfo |> Option.iter (fun (SynBindingReturnInfo (t, _, _)) -> walkType t)

//    and walkInterfaceImpl (InterfaceImpl(_, bindings, _)) = List.iter walkBinding bindings

//    and walkIndexerArg = function
//        | SynIndexerArg.One(e,_fromEnd,_range) -> walkExpr e
//        | SynIndexerArg.Two(e1,_e1FromEnd,e2,_e2FromEnd,_e1Range,_e2Range) -> List.iter walkExpr [e1; e2]

//    and walkType = function
//        | SynType.Array (_, t, _)
//        | SynType.HashConstraint (t, _)
//        | SynType.MeasurePower (t, _, _) -> walkType t
//        | SynType.Fun (t1, t2, _)
//        | SynType.MeasureDivide (t1, t2, _) -> walkType t1; walkType t2
//        | SynType.App (ty, _, types, _, _, _, _) -> walkType ty; List.iter walkType types
//        | SynType.LongIdentApp (_, _, _, types, _, _, _) -> List.iter walkType types
//        | SynType.Tuple (_, ts, _) -> ts |> List.iter (fun (_, t) -> walkType t)
//        | SynType.WithGlobalConstraints (t, typeConstraints, _) ->
//            walkType t; List.iter walkTypeConstraint typeConstraints
//        | _ -> ()

//    and walkClause (Clause (pat, e1, e2, _, _)) =
//        walkPat pat
//        walkExpr e2
//        e1 |> Option.iter walkExpr

//    and walkSimplePats = function
//        | SynSimplePats.SimplePats (pats, _) -> List.iter walkSimplePat pats
//        | SynSimplePats.Typed (pats, ty, ran) ->
//            if isInside ran then result <- true
//            walkSimplePats pats
//            walkType ty

//    and walkExpr = function
//        | SynExpr.Typed (e, _, ran) ->
//            if isInside ran then result <- true
//            walkExpr e
//        | SynExpr.Paren (e, _, _, _)
//        | SynExpr.Quote (_, _, e, _, _)
//        | SynExpr.InferredUpcast (e, _)
//        | SynExpr.InferredDowncast (e, _)
//        | SynExpr.AddressOf (_, e, _, _)
//        | SynExpr.DoBang (e, _)
//        | SynExpr.YieldOrReturn (_, e, _)
//        | SynExpr.ArrayOrListOfSeqExpr (_, e, _)
//        | SynExpr.CompExpr (_, _, e, _)
//        | SynExpr.Do (e, _)
//        | SynExpr.Assert (e, _)
//        | SynExpr.Lazy (e, _)
//        | SynExpr.YieldOrReturnFrom (_, e, _) -> walkExpr e
//        | SynExpr.Lambda (_, _, pats, e, _) ->
//            walkSimplePats pats
//            walkExpr e
//        | SynExpr.New (_, t, e, _)
//        | SynExpr.TypeTest (e, t, _)
//        | SynExpr.Upcast (e, t, _)
//        | SynExpr.Downcast (e, t, _) -> walkExpr e; walkType t
//        | SynExpr.Tuple (_, es, _, _)
//        | Sequentials es
//        | SynExpr.ArrayOrList (_, es, _) -> List.iter walkExpr es
//        | SynExpr.App (_, _, e1, e2, _)
//        | SynExpr.TryFinally (e1, e2, _, _, _)
//        | SynExpr.While (_, e1, e2, _) -> List.iter walkExpr [e1; e2]
//        | SynExpr.Record (_, _, fields, _) ->
//            fields |> List.iter (fun ((ident, _), e, _) ->
//                        e |> Option.iter walkExpr)
//        | SynExpr.ObjExpr(ty, argOpt, bindings, ifaces, _, _) ->
//            argOpt |> Option.iter (fun (e, ident) ->
//                walkExpr e)
//            walkType ty
//            List.iter walkBinding bindings
//            List.iter walkInterfaceImpl ifaces
//        | SynExpr.For (_, ident, e1, _, e2, e3, _) ->
//            List.iter walkExpr [e1; e2; e3]
//        | SynExpr.ForEach (_, _, _, pat, e1, e2, _) ->
//            walkPat pat
//            List.iter walkExpr [e1; e2]
//        | SynExpr.MatchLambda (_, _, synMatchClauseList, _, _) ->
//            List.iter walkClause synMatchClauseList
//        | SynExpr.Match (_, e, synMatchClauseList, _) ->
//            walkExpr e
//            List.iter walkClause synMatchClauseList
//        | SynExpr.TypeApp (e, _, tys, _, _, _, _) ->
//            List.iter walkType tys; walkExpr e
//        | SynExpr.LetOrUse (_, _, bindings, e, _) ->
//            List.iter walkBinding bindings; walkExpr e
//        | SynExpr.TryWith (e, _, clauses, _, _, _, _) ->
//            List.iter walkClause clauses;  walkExpr e
//        | SynExpr.IfThenElse (e1, e2, e3, _, _, _, _) ->
//            List.iter walkExpr [e1; e2]
//            e3 |> Option.iter walkExpr
//        | SynExpr.LongIdentSet (ident, e, _)
//        | SynExpr.DotGet (e, _, ident, _) ->
//            walkExpr e
//        | SynExpr.DotSet (e1, idents, e2, _) ->
//            walkExpr e1
//            walkExpr e2
//        | SynExpr.DotIndexedGet (e, args, _, _) ->
//            walkExpr e
//            List.iter walkIndexerArg args
//        | SynExpr.DotIndexedSet (e1, args, e2, _, _, _) ->
//            walkExpr e1
//            List.iter walkIndexerArg args
//            walkExpr e2
//        | SynExpr.NamedIndexedPropertySet (ident, e1, e2, _) ->
//            List.iter walkExpr [e1; e2]
//        | SynExpr.DotNamedIndexedPropertySet (e1, ident, e2, e3, _) ->
//            List.iter walkExpr [e1; e2; e3]
//        | SynExpr.JoinIn (e1, _, e2, _) -> List.iter walkExpr [e1; e2]
//        | SynExpr.LetOrUseBang (_, _, _, pat, e1, ands, e2, _) ->
//            walkPat pat
//            walkExpr e1
//            for (_,_,_,pat,body,_) in ands do
//              walkPat pat
//              walkExpr body
//            walkExpr e2
//        | SynExpr.TraitCall (ts, sign, e, _) ->
//            List.iter walkTypar ts
//            walkMemberSig sign
//            walkExpr e
//        | SynExpr.Const (SynConst.Measure(_, m), _) -> walkMeasure m
//        | _ -> ()

//    and walkMeasure = function
//        | SynMeasure.Product (m1, m2, _)
//        | SynMeasure.Divide (m1, m2, _) -> walkMeasure m1; walkMeasure m2
//        | SynMeasure.Named (longIdent, _) -> ()
//        | SynMeasure.Seq (ms, _) -> List.iter walkMeasure ms
//        | SynMeasure.Power (m, _, _) -> walkMeasure m
//        | SynMeasure.Var (ty, _) -> walkTypar ty
//        | SynMeasure.One
//        | SynMeasure.Anon _ -> ()

//    and walkSimplePat = function
//        | SynSimplePat.Attrib (pat, AllAttrs attrs, _) ->
//            walkSimplePat pat
//            List.iter walkAttribute attrs
//        | SynSimplePat.Typed(pat, t, ran) ->
//            if isInside ran then result <- true
//            walkSimplePat pat
//            walkType t
//        | _ -> ()

//    and walkField (SynField.Field(AllAttrs attrs, _, _, t, _, _, _, _)) =
//        List.iter walkAttribute attrs
//        walkType t

//    and walkValSig (SynValSig.ValSpfn(AllAttrs attrs, _, _, t, SynValInfo(argInfos, argInfo), _, _, _, _, _, _)) =
//        List.iter walkAttribute attrs
//        walkType t
//        argInfo :: (argInfos |> List.concat)
//        |> List.collect (fun (SynArgInfo(AllAttrs attrs, _, _)) -> attrs)
//        |> List.iter walkAttribute

//    and walkMemberSig = function
//        | SynMemberSig.Inherit (t, _)
//        | SynMemberSig.Interface(t, _) -> walkType t
//        | SynMemberSig.Member(vs, _, _) -> walkValSig vs
//        | SynMemberSig.ValField(f, _) -> walkField f
//        | SynMemberSig.NestedType(SynTypeDefnSig.TypeDefnSig (info, repr, memberSigs, _), _) ->
//            let isTypeExtensionOrAlias =
//                match repr with
//                | SynTypeDefnSigRepr.Simple(SynTypeDefnSimpleRepr.TypeAbbrev _, _)
//                | SynTypeDefnSigRepr.ObjectModel(SynTypeDefnKind.TyconAbbrev, _, _)
//                | SynTypeDefnSigRepr.ObjectModel(SynTypeDefnKind.TyconAugmentation, _, _) -> true
//                | _ -> false
//            walkComponentInfo isTypeExtensionOrAlias info
//            walkTypeDefnSigRepr repr
//            List.iter walkMemberSig memberSigs

//    and walkMember = function
//        | SynMemberDefn.AbstractSlot (valSig, _, _) -> walkValSig valSig
//        | SynMemberDefn.Member (binding, _) -> walkBinding binding
//        | SynMemberDefn.ImplicitCtor (_, AllAttrs attrs, AllSimplePats pats, _, _) ->
//            List.iter walkAttribute attrs
//            List.iter walkSimplePat pats
//        | SynMemberDefn.ImplicitInherit (t, e, _, _) -> walkType t; walkExpr e
//        | SynMemberDefn.LetBindings (bindings, _, _, _) -> List.iter walkBinding bindings
//        | SynMemberDefn.Interface (t, members, _) ->
//            walkType t
//            members |> Option.iter (List.iter walkMember)
//        | SynMemberDefn.Inherit (t, _, _) -> walkType t
//        | SynMemberDefn.ValField (field, _) -> walkField field
//        | SynMemberDefn.NestedType (tdef, _, _) -> walkTypeDefn tdef
//        | SynMemberDefn.AutoProperty (AllAttrs attrs, _, _, t, _, _, _, _, e, _, _) ->
//            List.iter walkAttribute attrs
//            Option.iter walkType t
//            walkExpr e
//        | _ -> ()

//    and walkEnumCase (EnumCase(AllAttrs attrs, _, _, _, _)) = List.iter walkAttribute attrs

//    and walkUnionCaseType = function
//        | SynUnionCaseType.UnionCaseFields fields -> List.iter walkField fields
//        | SynUnionCaseType.UnionCaseFullType (t, _) -> walkType t

//    and walkUnionCase (SynUnionCase.UnionCase (AllAttrs attrs, _, t, _, _, _)) =
//        List.iter walkAttribute attrs
//        walkUnionCaseType t

//    and walkTypeDefnSimple = function
//        | SynTypeDefnSimpleRepr.Enum (cases, _) -> List.iter walkEnumCase cases
//        | SynTypeDefnSimpleRepr.Union (_, cases, _) -> List.iter walkUnionCase cases
//        | SynTypeDefnSimpleRepr.Record (_, fields, _) -> List.iter walkField fields
//        | SynTypeDefnSimpleRepr.TypeAbbrev (_, t, _) -> walkType t
//        | _ -> ()

//    and walkComponentInfo isTypeExtensionOrAlias (ComponentInfo(AllAttrs attrs, typars, constraints, longIdent, _, _, _, _)) =
//        List.iter walkAttribute attrs
//        List.iter walkTyparDecl typars
//        List.iter walkTypeConstraint constraints

//    and walkTypeDefnRepr = function
//        | SynTypeDefnRepr.ObjectModel (_, defns, _) -> List.iter walkMember defns
//        | SynTypeDefnRepr.Simple(defn, _) -> walkTypeDefnSimple defn
//        | SynTypeDefnRepr.Exception _ -> ()

//    and walkTypeDefnSigRepr = function
//        | SynTypeDefnSigRepr.ObjectModel (_, defns, _) -> List.iter walkMemberSig defns
//        | SynTypeDefnSigRepr.Simple(defn, _) -> walkTypeDefnSimple defn
//        | SynTypeDefnSigRepr.Exception _ -> ()

//    and walkTypeDefn (TypeDefn (info, repr, members, _)) =
//        let isTypeExtensionOrAlias =
//            match repr with
//            | SynTypeDefnRepr.ObjectModel (SynTypeDefnKind.TyconAugmentation, _, _)
//            | SynTypeDefnRepr.ObjectModel (SynTypeDefnKind.TyconAbbrev, _, _)
//            | SynTypeDefnRepr.Simple (SynTypeDefnSimpleRepr.TypeAbbrev _, _) -> true
//            | _ -> false
//        walkComponentInfo isTypeExtensionOrAlias info
//        walkTypeDefnRepr repr
//        List.iter walkMember members

//    and walkSynModuleDecl (decl: SynModuleDecl) =
//        match decl with
//        | SynModuleDecl.NamespaceFragment fragment -> walkSynModuleOrNamespace fragment
//        | SynModuleDecl.NestedModule (info, _, modules, _, _) ->
//            walkComponentInfo false info
//            List.iter walkSynModuleDecl modules
//        | SynModuleDecl.Let (_, bindings, _) -> List.iter walkBinding bindings
//        | SynModuleDecl.DoExpr (_, expr, _) -> walkExpr expr
//        | SynModuleDecl.Types (types, _) -> List.iter walkTypeDefn types
//        | SynModuleDecl.Attributes (AllAttrs attrs, _) -> List.iter walkAttribute attrs
//        | _ -> ()

//    match input with
//    | Some (ParsedInput.ImplFile input) ->
//         walkImplFileInput input
//    | _ -> ()
//    //debug "%A" idents
//    result

///// Gives all ranges for current position
//let internal getRangesAtPosition (input: ParsedInput option) (r: pos) : range list =
//    let mutable result = []


//    let addIfInside (ran : range) =
//        let addToResult r =
//            result <- r::result

//        let isInside (ran : range) =
//            Range.rangeContainsPos ran r

//        if isInside ran then addToResult ran



//    let rec walkImplFileInput (ParsedImplFileInput(_, _, _, _, _, moduleOrNamespaceList, _)) =
//        List.iter walkSynModuleOrNamespace moduleOrNamespaceList

//    and walkSynModuleOrNamespace (SynModuleOrNamespace(_, _, _, decls, _, AllAttrs attrs, _, r)) =
//        addIfInside r
//        List.iter walkAttribute attrs
//        List.iter walkSynModuleDecl decls

//    and walkAttribute (attr: SynAttribute) =
//        addIfInside attr.Range
//        walkExpr attr.ArgExpr

//    and walkTyparDecl (SynTyparDecl.TyparDecl (AllAttrs attrs, typar)) =
//        List.iter walkAttribute attrs
//        walkTypar typar

//    and walkTypeConstraint = function
//        | SynTypeConstraint.WhereTyparIsValueType (t, r)
//        | SynTypeConstraint.WhereTyparIsReferenceType (t, r)
//        | SynTypeConstraint.WhereTyparIsUnmanaged (t, r)
//        | SynTypeConstraint.WhereTyparSupportsNull (t, r)
//        | SynTypeConstraint.WhereTyparIsComparable (t, r)
//        | SynTypeConstraint.WhereTyparIsEquatable (t, r) ->
//            addIfInside r
//            walkTypar t
//        | SynTypeConstraint.WhereTyparDefaultsToType (t, ty, r)
//        | SynTypeConstraint.WhereTyparSubtypeOfType (t, ty, r) ->
//            addIfInside r
//            walkTypar t; walkType ty
//        | SynTypeConstraint.WhereTyparIsEnum (t, ts, r)
//        | SynTypeConstraint.WhereTyparIsDelegate (t, ts, r) ->
//            addIfInside r
//            walkTypar t; List.iter walkType ts
//        | SynTypeConstraint.WhereTyparSupportsMember (ts, sign, r) ->
//            addIfInside r
//            List.iter walkType ts; walkMemberSig sign

//    and walkPat = function
//        | SynPat.Tuple (_, pats, r)
//        | SynPat.ArrayOrList (_, pats, r)
//        | SynPat.Ands (pats, r) ->
//            addIfInside r
//            List.iter walkPat pats
//        | SynPat.Named (pat, ident, _, _, r) ->
//            addIfInside r
//            walkPat pat
//        | SynPat.Typed (pat, t, r) ->
//            addIfInside r
//            walkPat pat
//            walkType t
//        | SynPat.Attrib (pat, AllAttrs attrs, r) ->
//            addIfInside r
//            walkPat pat
//            List.iter walkAttribute attrs
//        | SynPat.Or (pat1, pat2, r) ->
//            addIfInside r
//            List.iter walkPat [pat1; pat2]
//        | SynPat.LongIdent (ident, _, typars, ConstructorPats pats, _, r) ->
//            addIfInside r
//            typars
//            |> Option.iter (fun (SynValTyparDecls (typars, _, constraints)) ->
//                    List.iter walkTyparDecl typars
//                    List.iter walkTypeConstraint constraints)
//            List.iter walkPat pats
//        | SynPat.Paren (pat, r) ->
//            addIfInside r
//            walkPat pat
//        | SynPat.IsInst (t, r) ->
//            addIfInside r
//            walkType t
//        | SynPat.QuoteExpr(e, r) ->
//            addIfInside r
//            walkExpr e
//        | SynPat.Const(_, r) -> addIfInside r
//        | SynPat.Wild(r) -> addIfInside r
//        | SynPat.Record(_, r) -> addIfInside r
//        | SynPat.Null(r) -> addIfInside r
//        | SynPat.OptionalVal(_, r) -> addIfInside r
//        | SynPat.DeprecatedCharRange(_, _, r) -> addIfInside r
//        | SynPat.InstanceMember(_, _, _, accessibility, r) -> addIfInside r
//        | SynPat.FromParseError(_, r) ->addIfInside r

//    and walkTypar (Typar (_, _, _)) = ()

//    and walkBinding (SynBinding.Binding (_, _, _, _, AllAttrs attrs, _, _, pat, returnInfo, e, r, _)) =
//        addIfInside r
//        List.iter walkAttribute attrs
//        walkPat pat
//        walkExpr e
//        returnInfo |> Option.iter (fun (SynBindingReturnInfo (t, r, _)) -> addIfInside r; walkType t)

//    and walkInterfaceImpl (InterfaceImpl(_, bindings, r)) =
//        addIfInside r
//        List.iter walkBinding bindings

//    and walkIndexerArg = function
//        | SynIndexerArg.One(e,_fromEnd,_range) -> walkExpr e
//        | SynIndexerArg.Two(e1,_e1FromEnd,e2,_e2FromEnd,_e1Range,_e2Range) -> List.iter walkExpr [e1; e2]

//    and walkType = function
//        | SynType.Array (_, t, r)
//        | SynType.HashConstraint (t, r)
//        | SynType.MeasurePower (t, _, r) ->
//            addIfInside r
//            walkType t
//        | SynType.Fun (t1, t2, r)
//        | SynType.MeasureDivide (t1, t2, r) ->
//            addIfInside r
//            walkType t1; walkType t2
//        | SynType.App (ty, _, types, _, _, _, r) ->
//            addIfInside r
//            walkType ty; List.iter walkType types
//        | SynType.LongIdentApp (_, _, _, types, _, _, r) ->
//            addIfInside r
//            List.iter walkType types
//        | SynType.Tuple (_, ts, r) ->
//            addIfInside r
//            ts |> List.iter (fun (_, t) -> walkType t)
//        | SynType.WithGlobalConstraints (t, typeConstraints, r) ->
//            addIfInside r
//            walkType t; List.iter walkTypeConstraint typeConstraints
//        | SynType.LongIdent(longDotId) -> ()
//        | SynType.AnonRecd(isStruct, typeNames, r) -> addIfInside r
//        | SynType.Var(genericName, r) -> addIfInside r
//        | SynType.Anon(r) -> addIfInside r
//        | SynType.StaticConstant(constant, r) -> addIfInside r
//        | SynType.StaticConstantExpr(expr, r) -> addIfInside r
//        | SynType.StaticConstantNamed(expr, _, r) -> addIfInside r
//        | SynType.Paren(innerType, r) ->
//          addIfInside r
//          walkType innerType


//    and walkClause (Clause (pat, e1, e2, r, _)) =
//        addIfInside r
//        walkPat pat
//        walkExpr e2
//        e1 |> Option.iter walkExpr

//    and walkSimplePats = function
//        | SynSimplePats.SimplePats (pats, r) ->
//            addIfInside r
//            List.iter walkSimplePat pats
//        | SynSimplePats.Typed (pats, ty, r) ->
//            addIfInside r
//            walkSimplePats pats
//            walkType ty

//    and walkExpr = function
//        | SynExpr.Typed (e, _, r) ->
//            addIfInside r
//            walkExpr e
//        | SynExpr.Paren (e, _, _, r)
//        | SynExpr.Quote (_, _, e, _, r)
//        | SynExpr.InferredUpcast (e, r)
//        | SynExpr.InferredDowncast (e, r)
//        | SynExpr.AddressOf (_, e, _, r)
//        | SynExpr.DoBang (e, r)
//        | SynExpr.YieldOrReturn (_, e, r)
//        | SynExpr.ArrayOrListOfSeqExpr (_, e, r)
//        | SynExpr.CompExpr (_, _, e, r)
//        | SynExpr.Do (e, r)
//        | SynExpr.Assert (e, r)
//        | SynExpr.Lazy (e, r)
//        | SynExpr.YieldOrReturnFrom (_, e, r) ->
//            addIfInside r
//            walkExpr e
//        | SynExpr.SequentialOrImplicitYield(_, e1, e2, ifNotE, r) ->
//            addIfInside r
//            walkExpr e1
//            walkExpr e2
//            walkExpr ifNotE
//        | SynExpr.Lambda (_, _, pats, e, r) ->
//            addIfInside r
//            walkSimplePats pats
//            walkExpr e
//        | SynExpr.New (_, t, e, r)
//        | SynExpr.TypeTest (e, t, r)
//        | SynExpr.Upcast (e, t, r)
//        | SynExpr.Downcast (e, t, r) ->
//            addIfInside r
//            walkExpr e; walkType t
//        | SynExpr.Tuple (_, es, _, _)
//        | Sequentials es -> List.iter walkExpr es //TODO??
//        | SynExpr.ArrayOrList (_, es, r) ->
//            addIfInside r
//            List.iter walkExpr es
//        | SynExpr.App (_, _, e1, e2, r)
//        | SynExpr.TryFinally (e1, e2, r, _, _)
//        | SynExpr.While (_, e1, e2, r) ->
//            addIfInside r
//            List.iter walkExpr [e1; e2]
//        | SynExpr.Record (_, _, fields, r) ->
//            addIfInside r
//            fields |> List.iter (fun ((ident, _), e, _) ->
//                        e |> Option.iter walkExpr)
//        | SynExpr.ObjExpr(ty, argOpt, bindings, ifaces, _, r) ->
//            addIfInside r
//            argOpt |> Option.iter (fun (e, ident) ->
//                walkExpr e)
//            walkType ty
//            List.iter walkBinding bindings
//            List.iter walkInterfaceImpl ifaces
//        | SynExpr.For (_, ident, e1, _, e2, e3, r) ->
//            addIfInside r
//            List.iter walkExpr [e1; e2; e3]
//        | SynExpr.ForEach (_, _, _, pat, e1, e2, r) ->
//            addIfInside r
//            walkPat pat
//            List.iter walkExpr [e1; e2]
//        | SynExpr.MatchLambda (_, _, synMatchClauseList, _, r) ->
//            addIfInside r
//            List.iter walkClause synMatchClauseList
//        | SynExpr.Match (_, e, synMatchClauseList, r) ->
//            addIfInside r
//            walkExpr e
//            List.iter walkClause synMatchClauseList
//        | SynExpr.TypeApp (e, _, tys, _, _, tr, r) ->
//            addIfInside tr
//            addIfInside r
//            List.iter walkType tys; walkExpr e
//        | SynExpr.LetOrUse (_, _, bindings, e, r) ->
//            addIfInside r
//            List.iter walkBinding bindings; walkExpr e
//        | SynExpr.TryWith (e, _, clauses, r, _, _, _) ->
//            addIfInside r
//            List.iter walkClause clauses;  walkExpr e
//        | SynExpr.IfThenElse (e1, e2, e3, _, _, _, r) ->
//            addIfInside r
//            List.iter walkExpr [e1; e2]
//            e3 |> Option.iter walkExpr
//        | SynExpr.LongIdentSet (ident, e, r)
//        | SynExpr.DotGet (e, _, ident, r) ->
//            addIfInside r
//            walkExpr e
//        | SynExpr.DotSet (e1, idents, e2, r) ->
//            addIfInside r
//            walkExpr e1
//            walkExpr e2
//        | SynExpr.DotIndexedGet (e, args, _, r) ->
//            addIfInside r
//            walkExpr e
//            List.iter walkIndexerArg args
//        | SynExpr.DotIndexedSet (e1, args, e2, _, _, r) ->
//            addIfInside r
//            walkExpr e1
//            List.iter walkIndexerArg args
//            walkExpr e2
//        | SynExpr.NamedIndexedPropertySet (ident, e1, e2, r) ->
//            addIfInside r
//            List.iter walkExpr [e1; e2]
//        | SynExpr.DotNamedIndexedPropertySet (e1, ident, e2, e3, r) ->
//            addIfInside r
//            List.iter walkExpr [e1; e2; e3]
//        | SynExpr.JoinIn (e1, _, e2, r) ->
//            addIfInside r
//            List.iter walkExpr [e1; e2]
//        | SynExpr.LetOrUseBang (_, _, _, pat, e1, ands, e2, r) ->
//            addIfInside r
//            walkPat pat
//            walkExpr e1
//            for (_,_,_,pat,body,r) in ands do
//              addIfInside r
//              walkPat pat
//              walkExpr body
//            walkExpr e2
//        | SynExpr.TraitCall (ts, sign, e, r) ->
//            addIfInside r
//            List.iter walkTypar ts
//            walkMemberSig sign
//            walkExpr e
//        | SynExpr.Const (SynConst.Measure(_, m), r) ->
//            addIfInside r
//            walkMeasure m
//        | SynExpr.Const (_, r) ->
//            addIfInside r
//        | SynExpr.AnonRecd(isStruct, copyInfo, recordFields, r) -> addIfInside r
//        | SynExpr.Sequential(seqPoint, isTrueSeq, expr1, expr2, r) -> ()
//        | SynExpr.Ident(_) -> ()
//        | SynExpr.LongIdent(isOptional, longDotId, altNameRefCell, r) -> addIfInside r
//        | SynExpr.Set(_, _, r) -> addIfInside r
//        | SynExpr.Null(r) -> addIfInside r
//        | SynExpr.ImplicitZero(r) -> addIfInside r
//        | SynExpr.MatchBang(matchSeqPoint, expr, clauses, r) -> addIfInside r
//        | SynExpr.LibraryOnlyILAssembly(_, _, _, _, r) -> addIfInside r
//        | SynExpr.LibraryOnlyStaticOptimization(_, _, _, r) -> addIfInside r
//        | SynExpr.LibraryOnlyUnionCaseFieldGet(expr, longId, _, r) -> addIfInside r
//        | SynExpr.LibraryOnlyUnionCaseFieldSet(_, longId, _, _, r) -> addIfInside r
//        | SynExpr.ArbitraryAfterError(debugStr, r) -> addIfInside r
//        | SynExpr.FromParseError(expr, r) -> addIfInside r
//        | SynExpr.DiscardAfterMissingQualificationAfterDot(_, r) -> addIfInside r
//        | SynExpr.Fixed(expr, r) -> addIfInside r

//    and walkMeasure = function
//        | SynMeasure.Product (m1, m2, r)
//        | SynMeasure.Divide (m1, m2, r) ->
//            addIfInside r
//            walkMeasure m1; walkMeasure m2
//        | SynMeasure.Named (longIdent, r) -> addIfInside r
//        | SynMeasure.Seq (ms, r) ->
//            addIfInside r
//            List.iter walkMeasure ms
//        | SynMeasure.Power (m, _, r) ->
//            addIfInside r
//            walkMeasure m
//        | SynMeasure.Var (ty, r) ->
//            addIfInside r
//            walkTypar ty
//        | SynMeasure.One
//        | SynMeasure.Anon _ -> ()

//    and walkSimplePat = function
//        | SynSimplePat.Attrib (pat, AllAttrs attrs, r) ->
//            addIfInside r
//            walkSimplePat pat
//            List.iter walkAttribute attrs
//        | SynSimplePat.Typed(pat, t, r) ->
//            addIfInside r
//            walkSimplePat pat
//            walkType t
//        | SynSimplePat.Id(ident, altNameRefCell, isCompilerGenerated, isThisVar, isOptArg, r) -> addIfInside r


//    and walkField (SynField.Field(AllAttrs attrs, _, _, t, _, _, _, r)) =
//        addIfInside r
//        List.iter walkAttribute attrs
//        walkType t

//    and walkValSig (SynValSig.ValSpfn(AllAttrs attrs, _, _, t, SynValInfo(argInfos, argInfo), _, _, _, _, _, r)) =
//        addIfInside r
//        List.iter walkAttribute attrs
//        walkType t
//        argInfo :: (argInfos |> List.concat)
//        |> List.collect (fun (SynArgInfo(AllAttrs attrs, _, _)) -> attrs)
//        |> List.iter walkAttribute

//    and walkMemberSig = function
//        | SynMemberSig.Inherit (t, r)
//        | SynMemberSig.Interface(t, r) ->
//            addIfInside r
//            walkType t
//        | SynMemberSig.Member(vs, _, r) ->
//            addIfInside r
//            walkValSig vs
//        | SynMemberSig.ValField(f, r) ->
//            addIfInside r
//            walkField f
//        | SynMemberSig.NestedType(SynTypeDefnSig.TypeDefnSig (info, repr, memberSigs, _), r) ->
//            addIfInside r
//            let isTypeExtensionOrAlias =
//                match repr with
//                | SynTypeDefnSigRepr.Simple(SynTypeDefnSimpleRepr.TypeAbbrev _, _)
//                | SynTypeDefnSigRepr.ObjectModel(SynTypeDefnKind.TyconAbbrev, _, _)
//                | SynTypeDefnSigRepr.ObjectModel(SynTypeDefnKind.TyconAugmentation, _, _) -> true
//                | _ -> false
//            walkComponentInfo isTypeExtensionOrAlias info
//            walkTypeDefnSigRepr repr
//            List.iter walkMemberSig memberSigs

//    and walkMember = function
//        | SynMemberDefn.AbstractSlot (valSig, _, r) ->
//            addIfInside r
//            walkValSig valSig
//        | SynMemberDefn.Member (binding, r) ->
//            addIfInside r
//            walkBinding binding
//        | SynMemberDefn.ImplicitCtor (_, AllAttrs attrs, AllSimplePats pats, _, r) ->
//            addIfInside r
//            List.iter walkAttribute attrs
//            List.iter walkSimplePat pats
//        | SynMemberDefn.ImplicitInherit (t, e, _, r) ->
//            addIfInside r
//            walkType t; walkExpr e
//        | SynMemberDefn.LetBindings (bindings, _, _, r) ->
//            addIfInside r
//            List.iter walkBinding bindings
//        | SynMemberDefn.Interface (t, members, r) ->
//            addIfInside r
//            walkType t
//            members |> Option.iter (List.iter walkMember)
//        | SynMemberDefn.Inherit (t, _, r) ->
//            addIfInside r
//            walkType t
//        | SynMemberDefn.ValField (field, r) ->
//            addIfInside r
//            walkField field
//        | SynMemberDefn.NestedType (tdef, _, r) ->
//            addIfInside r
//            walkTypeDefn tdef
//        | SynMemberDefn.AutoProperty (AllAttrs attrs, _, _, t, _, _, _, _, e, _, r) ->
//            addIfInside r
//            List.iter walkAttribute attrs
//            Option.iter walkType t
//            walkExpr e
//        | SynMemberDefn.Open(longId, r) -> addIfInside r

//    and walkEnumCase (EnumCase(AllAttrs attrs, _, _, _, r)) =
//        addIfInside r
//        List.iter walkAttribute attrs

//    and walkUnionCaseType = function
//        | SynUnionCaseType.UnionCaseFields fields -> List.iter walkField fields
//        | SynUnionCaseType.UnionCaseFullType (t, _) -> walkType t

//    and walkUnionCase (SynUnionCase.UnionCase (AllAttrs attrs, _, t, _, _, r)) =
//        addIfInside r
//        List.iter walkAttribute attrs
//        walkUnionCaseType t

//    and walkTypeDefnSimple = function
//        | SynTypeDefnSimpleRepr.Enum (cases, r) ->
//            addIfInside r
//            List.iter walkEnumCase cases
//        | SynTypeDefnSimpleRepr.Union (_, cases, r) ->
//            addIfInside r
//            List.iter walkUnionCase cases
//        | SynTypeDefnSimpleRepr.Record (_, fields, r) ->
//            addIfInside r
//            List.iter walkField fields
//        | SynTypeDefnSimpleRepr.TypeAbbrev (_, t, r) ->
//            addIfInside r
//            walkType t
//        | SynTypeDefnSimpleRepr.General(_, _, _, _, _, _, _, r) -> addIfInside r
//        | SynTypeDefnSimpleRepr.LibraryOnlyILAssembly(_, r) -> addIfInside r
//        | SynTypeDefnSimpleRepr.None(r) -> addIfInside r
//        | SynTypeDefnSimpleRepr.Exception(_) -> ()

//    and walkComponentInfo isTypeExtensionOrAlias (ComponentInfo(AllAttrs attrs, typars, constraints, longIdent, _, _, _, r)) =
//        addIfInside r
//        List.iter walkAttribute attrs
//        List.iter walkTyparDecl typars
//        List.iter walkTypeConstraint constraints

//    and walkTypeDefnRepr = function
//        | SynTypeDefnRepr.ObjectModel (_, defns, r) ->
//            addIfInside r
//            List.iter walkMember defns
//        | SynTypeDefnRepr.Simple(defn, r) ->
//            addIfInside r
//            walkTypeDefnSimple defn
//        | SynTypeDefnRepr.Exception _ -> ()

//    and walkTypeDefnSigRepr = function
//        | SynTypeDefnSigRepr.ObjectModel (_, defns, _) -> List.iter walkMemberSig defns
//        | SynTypeDefnSigRepr.Simple(defn, _) -> walkTypeDefnSimple defn
//        | SynTypeDefnSigRepr.Exception _ -> ()

//    and walkTypeDefn (TypeDefn (info, repr, members, r)) =
//        addIfInside r
//        let isTypeExtensionOrAlias =
//            match repr with
//            | SynTypeDefnRepr.ObjectModel (SynTypeDefnKind.TyconAugmentation, _, _)
//            | SynTypeDefnRepr.ObjectModel (SynTypeDefnKind.TyconAbbrev, _, _)
//            | SynTypeDefnRepr.Simple (SynTypeDefnSimpleRepr.TypeAbbrev _, _) -> true
//            | _ -> false
//        walkComponentInfo isTypeExtensionOrAlias info
//        walkTypeDefnRepr repr
//        List.iter walkMember members

//    and walkSynModuleDecl (decl: SynModuleDecl) =
//        match decl with
//        | SynModuleDecl.NamespaceFragment fragment -> walkSynModuleOrNamespace fragment
//        | SynModuleDecl.NestedModule (info, _, modules, _, r) ->
//            addIfInside r
//            walkComponentInfo false info
//            List.iter walkSynModuleDecl modules
//        | SynModuleDecl.Let (_, bindings, r) ->
//            addIfInside r
//            List.iter walkBinding bindings
//        | SynModuleDecl.DoExpr (_, expr, r) ->
//            addIfInside r
//            walkExpr expr
//        | SynModuleDecl.Types (types, r) ->
//            addIfInside r
//            List.iter walkTypeDefn types
//        | SynModuleDecl.Attributes (AllAttrs attrs, r) ->
//            addIfInside r
//            List.iter walkAttribute attrs
//        | SynModuleDecl.ModuleAbbrev(ident, longId, r) -> addIfInside r
//        | SynModuleDecl.Exception(_, r) -> addIfInside r
//        | SynModuleDecl.Open(longDotId, r) -> addIfInside r
//        | SynModuleDecl.HashDirective(_, r) -> addIfInside r

//    match input with
//    | Some (ParsedInput.ImplFile input) ->
//            walkImplFileInput input
//    | _ -> ()
//    //debug "%A" idents
//    result


//let getLongIdentAt ast pos =
//    let idents = getLongIdents (Some ast)
//    match idents.TryGetValue pos with
//    | true, idents -> Some idents
//    | _ -> None

///// Returns ranges of all quotations found in an untyped AST
//let getQuotationRanges ast =
//    let quotationRanges = ResizeArray()

//    let rec visitExpr = function
//        | SynExpr.LongIdentSet (_, expr, _)
//        | SynExpr.Typed (expr, _, _)
//        | SynExpr.Paren (expr, _, _, _)
//        | SynExpr.New (_, _, expr, _)
//        | SynExpr.ArrayOrListOfSeqExpr (_, expr, _)
//        | SynExpr.CompExpr (_, _, expr, _)
//        | SynExpr.ForEach (_, _, _, _, _, expr(*body*), _)
//        | SynExpr.YieldOrReturn (_, expr, _)
//        | SynExpr.YieldOrReturnFrom (_, expr, _)
//        | SynExpr.Do (expr, _)
//        | SynExpr.DoBang (expr, _)
//        | SynExpr.Downcast (expr, _, _)
//        | SynExpr.For (_, _, _, _, _, expr, _)
//        | SynExpr.Lazy (expr, _)
//        | SynExpr.Assert (expr, _)
//        | SynExpr.TypeApp (expr, _, _, _, _, _, _)
//        | SynExpr.DotSet (_, _, expr, _)
//        | SynExpr.DotIndexedSet (_, _, expr, _, _, _)
//        | SynExpr.NamedIndexedPropertySet (_, _, expr, _)
//        | SynExpr.DotNamedIndexedPropertySet (_, _, _, expr, _)
//        | SynExpr.TypeTest (expr, _, _)
//        | SynExpr.Upcast (expr, _, _)
//        | SynExpr.InferredUpcast (expr, _)
//        | SynExpr.InferredDowncast (expr, _)
//        | SynExpr.Lambda (_, _, _, expr, _)
//        | SynExpr.AddressOf (_, expr, _, _) ->
//            visitExpr expr
//        | SynExpr.App (_,_, expr1(*funcExpr*),expr2(*argExpr*), _)
//        | SynExpr.TryFinally (expr1, expr2, _, _, _)
//        | SynExpr.While (_, expr1, expr2, _) ->
//            visitExpr expr1; visitExpr expr2
//        | SynExpr.LetOrUseBang (_, _, _, _,expr1(*rhsExpr*),ands, expr2(*body*), _) ->
//          visitExpr expr1
//          for (_,_,_,_,body,_) in ands do
//            visitExpr body
//          visitExpr expr2
//        | SynExpr.Tuple (_, exprs, _, _)
//        | SynExpr.ArrayOrList (_, exprs, _)
//        | Sequentials  exprs ->
//            List.iter visitExpr exprs
//        | SynExpr.TryWith (expr, _, clauses, _, _, _, _)
//        | SynExpr.Match (_, expr, clauses, _) ->
//            visitExpr expr; visitMatches clauses
//        | SynExpr.IfThenElse (cond, trueBranch, falseBranchOpt, _, _, _, _) ->
//            visitExpr cond; visitExpr trueBranch
//            falseBranchOpt |> Option.iter visitExpr
//        | SynExpr.LetOrUse (_, _, bindings, body, _) -> visitBindindgs bindings; visitExpr body
//        | SynExpr.Quote (_, _isRaw, _quotedExpr, _, range) -> quotationRanges.Add range
//        | SynExpr.MatchLambda (_, _, clauses, _, _) -> visitMatches clauses
//        | SynExpr.ObjExpr (_, _, bindings, _, _ , _) -> visitBindindgs bindings
//        | SynExpr.Record (_, _, fields, _) ->
//            fields |> List.choose (fun (_, expr, _) -> expr) |> List.iter visitExpr
//        | _ -> ()

//    and visitBinding (Binding(_, _, _, _, _, _, _, _, _, body, _, _)) = visitExpr body
//    and visitBindindgs = List.iter visitBinding

//    and visitPattern = function
//        | SynPat.QuoteExpr (expr, _) -> visitExpr expr
//        | SynPat.Named (pat, _, _, _, _)
//        | SynPat.Paren (pat, _)
//        | SynPat.Typed (pat, _, _) -> visitPattern pat
//        | SynPat.Ands (pats, _)
//        | SynPat.Tuple (_, pats, _)
//        | SynPat.ArrayOrList (_, pats, _) -> List.iter visitPattern pats
//        | SynPat.Or (pat1, pat2, _) -> visitPattern pat1; visitPattern pat2
//        | SynPat.LongIdent (_, _, _, ctorArgs, _, _) ->
//            match ctorArgs with
//            | SynArgPats.Pats pats -> List.iter visitPattern pats
//            | SynArgPats.NamePatPairs(xs, _) ->
//                xs |> List.map snd |> List.iter visitPattern
//        | SynPat.Record(xs, _) -> xs |> List.map snd |> List.iter visitPattern
//        | _ -> ()

//    and visitMatch (SynMatchClause.Clause (pat, _, expr, _, _)) = visitPattern pat; visitExpr expr

//    and visitMatches = List.iter visitMatch

//    let visitMember = function
//        | SynMemberDefn.LetBindings (bindings, _, _, _) -> visitBindindgs bindings
//        | SynMemberDefn.Member (binding, _) -> visitBinding binding
//        | SynMemberDefn.AutoProperty (_, _, _, _, _, _, _, _, expr, _, _) -> visitExpr expr
//        | _ -> ()

//    let visitType ty =
//        let (SynTypeDefn.TypeDefn (_, repr, defns, _)) = ty
//        match repr with
//        | SynTypeDefnRepr.ObjectModel (_, objDefns, _) ->
//            for d in objDefns do visitMember d
//        | _ -> ()
//        for d in defns do visitMember d

//    let rec visitDeclarations decls =
//        decls |> List.iter
//           (function
//            | SynModuleDecl.Let (_, bindings, _) -> visitBindindgs bindings
//            | SynModuleDecl.DoExpr (_, expr, _) -> visitExpr expr
//            | SynModuleDecl.Types (types, _) -> List.iter visitType types
//            | SynModuleDecl.NestedModule (_, _, decls, _, _) -> visitDeclarations decls
//            | _ -> () )

//    let visitModulesAndNamespaces modulesOrNss =
//        modulesOrNss
//        |> Seq.iter (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _)) -> visitDeclarations decls)
//    ast
//    |> Option.iter (function
//        | ParsedInput.ImplFile (ParsedImplFileInput(_, _, _, _, _, modules, _)) -> visitModulesAndNamespaces modules
//        | _ -> ())
//    quotationRanges

///// Returns all string literal ranges
//let internal getStringLiterals ast : Range.range list =
//    let result = ResizeArray()

//    let visitType ty =
//        match ty with
//        | SynType.StaticConstant (SynConst.String(_, r), _) -> result.Add r
//        | _ -> ()

//    let rec visitExpr = function
//        | SynExpr.ArrayOrListOfSeqExpr (_, expr, _)
//        | SynExpr.CompExpr (_, _, expr, _)
//        | SynExpr.Lambda (_, _, _, expr, _)
//        | SynExpr.YieldOrReturn (_, expr, _)
//        | SynExpr.YieldOrReturnFrom (_, expr, _)
//        | SynExpr.New (_, _, expr, _)
//        | SynExpr.Assert (expr, _)
//        | SynExpr.Do (expr, _)
//        | SynExpr.Typed (expr, _, _)
//        | SynExpr.Paren (expr, _, _, _)
//        | SynExpr.DoBang (expr, _)
//        | SynExpr.Downcast (expr, _, _)
//        | SynExpr.For (_, _, _, _, _, expr, _)
//        | SynExpr.Lazy (expr, _)
//        | SynExpr.TypeTest(expr, _, _)
//        | SynExpr.Upcast(expr, _, _)
//        | SynExpr.InferredUpcast(expr, _)
//        | SynExpr.InferredDowncast(expr, _)
//        | SynExpr.LongIdentSet (_, expr, _)
//        | SynExpr.DotGet (expr, _, _, _)
//        | SynExpr.ForEach (_, _, _, _, _,expr(*body*), _) -> visitExpr expr
//        | SynExpr.App (_,_, expr1(*funcExpr*), expr2(*argExpr*), _)
//        | SynExpr.TryFinally (expr1, expr2, _, _, _)
//        | SynExpr.NamedIndexedPropertySet (_, expr1, expr2, _)
//        | SynExpr.DotNamedIndexedPropertySet (_, _, expr1, expr2, _)
//        | SynExpr.While (_, expr1, expr2, _) ->
//            visitExpr expr1; visitExpr expr2
//        | SynExpr.LetOrUseBang (_, _, _, _,expr1(*rhsExpr*), ands, expr2(*body*), _) ->
//            visitExpr expr1
//            for (_,_,_,_,body,_) in ands do
//              visitExpr body
//            visitExpr expr2
//        | Sequentials exprs
//        | SynExpr.Tuple (_, exprs, _, _)
//        | SynExpr.ArrayOrList(_, exprs, _) -> List.iter visitExpr exprs
//        | SynExpr.Match (_, expr, clauses, _)
//        | SynExpr.TryWith(expr, _, clauses, _, _, _, _) ->
//            visitExpr expr; visitMatches clauses
//        | SynExpr.IfThenElse(cond, trueBranch, falseBranchOpt, _, _, _, _) ->
//            visitExpr cond
//            visitExpr trueBranch
//            falseBranchOpt |> Option.iter visitExpr
//        | SynExpr.LetOrUse (_, _, bindings, body, _) ->
//            visitBindindgs bindings
//            visitExpr body
//        | SynExpr.Record (_, _, fields, _) ->
//            fields |> List.choose (fun (_, expr, _) -> expr) |> List.iter visitExpr
//        | SynExpr.MatchLambda (_, _, clauses, _, _) -> visitMatches clauses
//        | SynExpr.ObjExpr (_, _, bindings, _, _ , _) -> visitBindindgs bindings
//        | SynExpr.Const (SynConst.String (_, r), _) -> result.Add r
//        | SynExpr.TypeApp(_, _, tys, _, _, _, _) -> List.iter visitType tys
//        | _ -> ()

//    and visitBinding (Binding(_, _, _, _, _, _, _, _, _, body, _, _)) = visitExpr body
//    and visitBindindgs = List.iter visitBinding
//    and visitMatch (SynMatchClause.Clause (_, _, expr, _, _)) = visitExpr expr
//    and visitMatches = List.iter visitMatch

//    let visitMember = function
//        | SynMemberDefn.LetBindings (bindings, _, _, _) -> visitBindindgs bindings
//        | SynMemberDefn.Member (binding, _) -> visitBinding binding
//        | SynMemberDefn.AutoProperty (_, _, _, _, _, _, _, _, expr, _, _) -> visitExpr expr
//        | _ -> ()

//    let visitTypeDefn ty =
//        let (SynTypeDefn.TypeDefn (_, repr, memberDefns, _)) = ty
//        match repr with
//        | SynTypeDefnRepr.ObjectModel (_, defns, _) ->
//            for d in defns do visitMember d
//        | SynTypeDefnRepr.Simple(SynTypeDefnSimpleRepr.TypeAbbrev(_, SynType.App(_, _, tys, _,_ , _, _), _), _) ->
//            List.iter visitType tys
//        | _ -> ()
//        List.iter visitMember memberDefns

//    let rec visitDeclarations decls =
//        for declaration in decls do
//            match declaration with
//            | SynModuleDecl.Let (_, bindings, _) -> visitBindindgs bindings
//            | SynModuleDecl.DoExpr (_, expr, _) -> visitExpr expr
//            | SynModuleDecl.Types (types, _) -> for ty in types do visitTypeDefn ty
//            | SynModuleDecl.NestedModule (_, _, decls, _, _) -> visitDeclarations decls
//            | _ -> ()

//    let visitModulesAndNamespaces modulesOrNss =
//        Seq.iter (fun (SynModuleOrNamespace(_, _, _, decls, _, _, _, _)) -> visitDeclarations decls) modulesOrNss

//    ast
//    |> Option.iter (function
//        | ParsedInput.ImplFile (ParsedImplFileInput(_, _, _, _, _, modules, _)) -> visitModulesAndNamespaces modules
//        | _ -> ())

//    List.ofSeq result

///// Get path to containing module/namespace of a given position
//let getModuleOrNamespacePath (pos: pos) (ast: ParsedInput) =
//    let idents =
//        match ast with
//        | ParsedInput.ImplFile (ParsedImplFileInput(_, _, _, _, _, modules, _)) ->
//            let rec walkModuleOrNamespace idents (decls, moduleRange) =
//                decls
//                |> List.fold (fun acc ->
//                    function
//                    | SynModuleDecl.NestedModule (componentInfo, _, nestedModuleDecls, _, nestedModuleRange) ->
//                        if rangeContainsPos moduleRange pos then
//                            let (ComponentInfo(_,_,_,longIdent,_,_,_,_)) = componentInfo
//                            walkModuleOrNamespace (longIdent::acc) (nestedModuleDecls, nestedModuleRange)
//                        else acc
//                    | _ -> acc) idents

//            modules
//            |> List.fold (fun acc (SynModuleOrNamespace(longIdent, _, _, decls, _, _, _, moduleRange)) ->
//                    if rangeContainsPos moduleRange pos then
//                        walkModuleOrNamespace (longIdent::acc) (decls, moduleRange) @ acc
//                    else acc) []
//        | ParsedInput.SigFile(ParsedSigFileInput(_, _, _, _, modules)) ->
//            let rec walkModuleOrNamespaceSig idents (decls, moduleRange) =
//                decls
//                |> List.fold (fun acc ->
//                    function
//                    | SynModuleSigDecl.NestedModule (componentInfo, _, nestedModuleDecls, nestedModuleRange) ->
//                        if rangeContainsPos moduleRange pos then
//                            let (ComponentInfo(_,_,_,longIdent,_,_,_,_)) = componentInfo
//                            walkModuleOrNamespaceSig (longIdent::acc) (nestedModuleDecls, nestedModuleRange)
//                        else acc
//                    | _ -> acc) idents

//            modules
//            |> List.fold (fun acc (SynModuleOrNamespaceSig(longIdent, _, _, decls, _, _, _, moduleRange)) ->
//                    if rangeContainsPos moduleRange pos then
//                        walkModuleOrNamespaceSig (longIdent::acc) (decls, moduleRange) @ acc
//                    else acc) []
//    idents
//    |> List.rev
//    |> Seq.concat
//    |> Seq.map (fun ident -> ident.idText)
//    |> String.concat "."


//module HashDirectiveInfo =
//    open System.IO

//    type IncludeDirective =
//        | ResolvedDirectory of string

//    type LoadDirective =
//        | ExistingFile of string
//        | UnresolvableFile of string * previousIncludes : string array

//    [<NoComparison>]
//    type Directive =
//        | Include of IncludeDirective * range
//        | Load of LoadDirective * range

//    /// returns an array of LoadScriptResolutionEntries
//    /// based on #I and #load directives
//    let getIncludeAndLoadDirectives ast =
//        // the Load items are resolved using fallback resolution relying on previously parsed #I directives
//        // (this behaviour is undocumented in F# but it seems to be how it works).

//        // list of #I directives so far (populated while encountering those in order)
//        // TODO: replace by List.fold if possible
//        let includesSoFar = new System.Collections.Generic.List<_>()
//        let pushInclude = includesSoFar.Add

//        // those might need to be abstracted away from real filesystem operations
//        let fileExists = File.Exists
//        let directoryExists = Directory.Exists
//        let isPathRooted (path: string) = Path.IsPathRooted path
//        let getDirectoryOfFile = Path.GetFullPathSafe >> Path.GetDirectoryName
//        let getRootedDirectory = Path.GetFullPathSafe
//        let makeRootedDirectoryIfNecessary baseDirectory directory =
//            if not (isPathRooted directory) then
//                getRootedDirectory (baseDirectory </> directory)
//            else
//                directory

//        // separate function to reduce nesting one level
//        let parseDirectives modules file =
//            [|
//            let baseDirectory = getDirectoryOfFile file
//            for (SynModuleOrNamespace (_, _, _, declarations, _, _, _, _)) in modules do
//                for decl in declarations do
//                    match decl with
//                    | SynModuleDecl.HashDirective (ParsedHashDirective("I",[directory],range),_) ->
//                        let directory = makeRootedDirectoryIfNecessary (getDirectoryOfFile file) directory

//                        if directoryExists directory then
//                            let includeDirective = ResolvedDirectory(directory)
//                            pushInclude includeDirective
//                            yield Include (includeDirective, range)

//                    | SynModuleDecl.HashDirective (ParsedHashDirective ("load",files,range),_) ->
//                        for f in files do
//                            if isPathRooted f && fileExists f then

//                                // this is absolute reference to an existing script, easiest case
//                                yield Load (ExistingFile f, range)

//                            else
//                                // I'm not sure if the order is correct, first checking relative to file containing the #load directive
//                                // then checking for undocumented resolution using previously parsed #I directives
//                                let fileRelativeToCurrentFile = baseDirectory </> f
//                                if fileExists fileRelativeToCurrentFile then
//                                    // this is existing file relative to current file
//                                    yield Load (ExistingFile fileRelativeToCurrentFile, range)

//                                else
//                                    // match file against first include which seemingly have it found
//                                    let maybeFile =
//                                        includesSoFar
//                                        |> Seq.tryPick (function
//                                            | (ResolvedDirectory d) ->
//                                                let filePath = d </> f
//                                                if fileExists filePath then Some filePath else None
//                                        )
//                                    match maybeFile with
//                                    | None -> () // can't load this file even using any of the #I directives...
//                                    | Some f ->
//                                        yield Load (ExistingFile f,range)
//                    | _ -> ()
//            |]

//        match ast with
//        | ParsedInput.ImplFile (ParsedImplFileInput(fn,_,_,_,_,modules,_)) -> parseDirectives modules fn
//        | _ -> [||]

//    /// returns the Some (complete file name of a resolved #load directive at position) or None
//    let getHashLoadDirectiveResolvedPathAtPosition (pos: pos) (ast: ParsedInput) : string option =
//        getIncludeAndLoadDirectives ast
//        |> Array.tryPick (
//            function
//            | Load (ExistingFile f,range)
//                // check the line is within the range
//                // (doesn't work when there are multiple files given to a single #load directive)
//                when rangeContainsPos range pos
//                    -> Some f
//            | _     -> None
//        )


///// Set of visitor utilities, designed for the express purpose of fetching ranges
///// from an untyped AST for the purposes of outlining.
//module Outlining =
//    [<RequireQualifiedAccess>]
//    module private Range =
//        /// Create a range starting at the end of r1 and finishing at the end of r2
//        let inline endToEnd (r1: range) (r2: range) = mkFileIndexRange r1.FileIndex r1.End   r2.End

//        /// Create a range beginning at the start of r1 and finishing at the end of r2
//        let inline startToEnd (r1: range) (r2: range) = mkFileIndexRange r1.FileIndex r1.Start r2.End

//        /// Create a new range from r by shifting the starting column by m
//        let inline modStart (r: range) (m:int) =
//            let modstart = mkPos r.StartLine (r.StartColumn+m)
//            mkFileIndexRange r.FileIndex modstart r.End

//        /// Produce a new range by adding modStart to the StartColumn of `r`
//        /// and subtracting modEnd from the EndColumn of `r`
//        let inline modBoth (r:range) modStart modEnd =
//            let rStart = Range.mkPos r.StartLine (r.StartColumn+modStart)
//            let rEnd   = Range.mkPos r.EndLine   (r.EndColumn - modEnd)
//            mkFileIndexRange r.FileIndex rStart rEnd

//    /// Scope indicates the way a range/snapshot should be collapsed. |Scope.Scope.Same| is for a scope inside
//    /// some kind of scope delimiter, e.g. `[| ... |]`, `[ ... ]`, `{ ... }`, etc.  |Scope.Below| is for expressions
//    /// following a binding or the right hand side of a pattern, e.g. `let x = ...`
//    type Collapse =
//        | Below = 0
//        | Same = 1

//    type Scope =
//        | Open = 0
//        | Namespace = 1
//        | Module = 2
//        | Type = 3
//        | Member = 4
//        | LetOrUse = 5
//        | Match = 6
//        /// MatchLambda = function | expr -> .... | expr ->...
//        | MatchLambda = 7
//        | CompExpr = 8
//        | IfThenElse = 9
//        | ThenInIfThenElse = 10
//        | ElseInIfThenElse = 11
//        | TryWith = 12
//        | TryInTryWith = 13
//        | WithInTryWith = 14
//        | TryFinally = 15
//        | TryInTryFinally = 16
//        | FinallyInTryFinally = 17
//        | ArrayOrList = 18
//        | ObjExpr = 19
//        | For = 20
//        | While = 21
//        | CompExprInternal = 22
//        | Quote = 23
//        | Record = 24
//        | Tuple = 25
//        | SpecialFunc = 26
//        | Do = 27
//        | Lambda = 28
//        | MatchClause = 29
//        | Attribute = 30
//        | Interface = 31
//        | HashDirective = 32
//        | LetOrUseBang = 33
//        | TypeExtension = 34
//        | YieldOrReturn = 35
//        | YieldOrReturnBang = 36
//        | UnionCase = 37
//        | EnumCase = 38
//        | RecordField = 39
//        | SimpleType = 40
//        | RecordDefn = 41
//        | UnionDefn = 42
//        | Comment = 43
//        | XmlDocComment = 44

//    [<NoComparison; Struct>]
//    type ScopeRange (scope:Scope, collapse:Collapse, r:range) =
//        member __.Scope = scope
//        member __.Collapse = collapse
//        member __.Range = r

//    // Only yield a range that spans 2 or more lines
//    let inline private rcheck scope collapse (r: range) =
//        seq { if r.StartLine <> r.EndLine then
//                yield ScopeRange (scope, collapse, r) }

//    let rec private parseExpr expression =
//        seq {
//            match expression with
//            | SynExpr.Upcast (e,_,_)
//            | SynExpr.Downcast (e,_,_)
//            | SynExpr.AddressOf(_,e,_,_)
//            | SynExpr.InferredDowncast (e,_)
//            | SynExpr.InferredUpcast (e,_)
//            | SynExpr.DotGet (e,_,_,_)
//            | SynExpr.Do (e,_)
//            | SynExpr.DotSet (e,_,_,_)
//            | SynExpr.New (_,_,e,_)
//            | SynExpr.Typed (e,_,_)
//            | SynExpr.DotIndexedGet (e,_,_,_)
//            | SynExpr.DotIndexedSet (e,_,_,_,_,_) -> yield! parseExpr e
//            | SynExpr.YieldOrReturn (_,e,r) ->
//                yield! rcheck Scope.YieldOrReturn Collapse.Below r
//                yield! parseExpr e
//            | SynExpr.YieldOrReturnFrom (_,e,r) ->
//                yield! rcheck Scope.YieldOrReturnBang Collapse.Below r
//                yield! parseExpr e
//            | SynExpr.DoBang (e,r) ->
//                yield! rcheck Scope.Do Collapse.Below <| Range.modStart r 3
//                yield! parseExpr e
//            | SynExpr.LetOrUseBang (_,_,_,pat,e1,ands,e2,_) ->
//                // for `let!` or `use!` the pattern begins at the end of the keyword so that
//                // this scope can be used without adjustment if there is no `=` on the same line
//                // if there is an `=` the range will be adjusted during the tooltip creation
//                yield! rcheck Scope.LetOrUseBang Collapse.Below <| Range.endToEnd pat.Range e1.Range
//                yield! parseExpr e1
//                for (_,_,_,_,body,r) in ands do
//                  yield! rcheck Scope.LetOrUseBang Collapse.Below r
//                  yield! parseExpr body
//                yield! parseExpr e2
//            | SynExpr.For (_,_,_,_,_,e,r)
//            | SynExpr.ForEach (_,_,_,_,_,e,r) ->
//                yield! rcheck Scope.For Collapse.Below r
//                yield! parseExpr e
//            | SynExpr.LetOrUse (_,_,bindings, body,_) ->
//                yield! parseBindings bindings
//                yield! parseExpr body
//            | SynExpr.Match (debugPointAtBinding,_,clauses,r) ->
//                match debugPointAtBinding with
//                | DebugPointAtBinding pr ->
//                    yield! rcheck Scope.Match Collapse.Below <| Range.endToEnd pr r
//                | _ -> ()
//                yield! parseMatchClauses clauses
//            | SynExpr.MatchLambda (_,_,clauses,_,r) ->
//                yield! rcheck Scope.MatchLambda Collapse.Below <| Range.modStart r 8
//                yield! parseMatchClauses clauses
//            | SynExpr.App (atomicFlag,isInfix,funcExpr,argExpr,r) ->
//                // seq exprs, custom operators, etc
//                if ExprAtomicFlag.NonAtomic=atomicFlag && (not isInfix)
//                   && (function | SynExpr.Ident _ -> true | _ -> false) funcExpr
//                   // if the argExrp is a computation expression another match will handle the outlining
//                   // these cases must be removed to prevent creating unnecessary tags for the same scope
//                   && (function | SynExpr.CompExpr _ -> false | _ -> true) argExpr then
//                        yield! rcheck Scope.SpecialFunc Collapse.Below <| Range.endToEnd funcExpr.Range r
//                yield! parseExpr argExpr
//                yield! parseExpr funcExpr
//            | SynExpr.Sequential (_,_,e1,e2,_) ->
//                yield! parseExpr e1
//                yield! parseExpr e2
//            | SynExpr.ArrayOrListOfSeqExpr (isArray,e,r) ->
//                yield! rcheck  Scope.ArrayOrList Collapse.Same <| Range.modBoth r (if isArray then 2 else 1) (if isArray then 2 else 1)
//                yield! parseExpr e
//            | SynExpr.CompExpr (arrayOrList,_,e,r) ->
//                if arrayOrList then
//                    yield! parseExpr e
//                else  // exclude the opening { and closing } on the cexpr from collapsing
//                    yield! rcheck Scope.CompExpr Collapse.Same <| Range.modBoth r 1 1
//                yield! parseExpr e
//            | SynExpr.ObjExpr (_,_,bindings,_,newRange,wholeRange) ->
//                let r = mkFileIndexRange newRange.FileIndex newRange.End (Range.mkPos wholeRange.EndLine (wholeRange.EndColumn - 1))
//                yield! rcheck Scope.ObjExpr Collapse.Below r
//                yield! parseBindings bindings
//            | SynExpr.TryWith (e,_,matchClauses,tryRange,withRange,tryPoint,withPoint) ->
//                match tryPoint with
//                | DebugPointAtTry.Yes r ->
//                    yield! rcheck Scope.TryWith Collapse.Below <| Range.endToEnd r tryRange
//                | _ -> ()
//                match withPoint with
//                | DebugPointAtWith.Yes r ->
//                    yield! rcheck Scope.WithInTryWith Collapse.Below <| Range.endToEnd r withRange
//                | _ -> ()
//                yield! parseExpr e
//                yield! parseMatchClauses matchClauses
//            | SynExpr.TryFinally (tryExpr,finallyExpr,r,tryPoint,finallyPoint) ->
//                match tryPoint with
//                | DebugPointAtTry.Yes tryRange ->
//                    yield! rcheck Scope.TryFinally Collapse.Below <| Range.endToEnd tryRange r
//                | _ -> ()
//                match finallyPoint with
//                | DebugPointAtFinally.Yes finallyRange ->
//                    yield! rcheck  Scope.FinallyInTryFinally Collapse.Below <| Range.endToEnd finallyRange r
//                | _ -> ()
//                yield! parseExpr tryExpr
//                yield! parseExpr finallyExpr
//            | SynExpr.IfThenElse (e1,e2,e3,seqPointInfo,_,_,r) ->
//                // Outline the entire IfThenElse
//                yield! rcheck Scope.IfThenElse Collapse.Below r
//                // Outline the `then` scope
//                match seqPointInfo with
//                | DebugPointAtBinding rt ->
//                    yield! rcheck  Scope.ThenInIfThenElse Collapse.Below <| Range.endToEnd rt e2.Range
//                | _ -> ()
//                yield! parseExpr e1
//                yield! parseExpr e2
//                match e3 with
//                | Some e ->
//                    match e with // prevent double collapsing on elifs
//                    | SynExpr.IfThenElse (_,_,_,_,_,_,_) ->
//                        yield! parseExpr e
//                    | _ ->
//                        yield! rcheck Scope.ElseInIfThenElse Collapse.Same e.Range
//                        yield! parseExpr e
//                | None -> ()
//            | SynExpr.While (_,_,e,r) ->
//                yield! rcheck Scope.While Collapse.Below  r
//                yield! parseExpr e
//            | SynExpr.Lambda (_,_,pats,e,r) ->
//                match pats with
//                | SynSimplePats.SimplePats (_,pr)
//                | SynSimplePats.Typed (_,_,pr) ->
//                    yield! rcheck Scope.Lambda Collapse.Below <| Range.endToEnd pr r
//                yield! parseExpr e
//            | SynExpr.Lazy (e,r) ->
//                yield! rcheck Scope.SpecialFunc Collapse.Below r
//                yield! parseExpr e
//            | SynExpr.Quote (_,isRaw,e,_,r) ->
//                // subtract columns so the @@> or @> is not collapsed
//                yield! rcheck Scope.Quote Collapse.Same <| Range.modBoth r (if isRaw then 3 else 2) (if isRaw then 3 else 2)
//                yield! parseExpr e
//            | SynExpr.Tuple (_,es,_,r) ->
//                yield! rcheck Scope.Tuple Collapse.Same r
//                yield! Seq.collect parseExpr es
//            | SynExpr.Paren (e,_,_,_) ->
//                yield! parseExpr e
//            | SynExpr.Record (recCtor,recCopy,recordFields,r) ->
//                if recCtor.IsSome then
//                    let (_,ctorArgs,_,_,_) = recCtor.Value
//                    yield! parseExpr ctorArgs
//                if recCopy.IsSome then
//                    let (e,_) = recCopy.Value
//                    yield! parseExpr e
//                yield! recordFields |> (Seq.choose (fun (_,e,_) -> e) >> Seq.collect parseExpr)
//                // exclude the opening `{` and closing `}` of the record from collapsing
//                yield! rcheck Scope.Record Collapse.Same <| Range.modBoth r 1 1
//            | _ -> ()
//        }

//    and private parseMatchClause (SynMatchClause.Clause (synPat,_,e,_,_)) =
//        seq { yield! rcheck Scope.MatchClause Collapse.Same <| Range.startToEnd synPat.Range e.Range  // Collapse the scope after `->`
//              yield! parseExpr e }

//    and private parseMatchClauses = Seq.collect parseMatchClause

//    and private parseAttributes (attrs: SynAttribute list) =
//        seq{
//            let attrListRange =
//                if List.isEmpty attrs then Seq.empty else
//                rcheck Scope.Attribute Collapse.Same  <| Range.startToEnd (attrs.[0].Range) (attrs.[attrs.Length-1].ArgExpr.Range)
//            match  attrs with
//            | [] -> ()
//            | [_] -> yield! attrListRange
//            | hd::tl ->
//                yield! attrListRange
//                yield! parseExpr hd.ArgExpr
//                // If there are more than 2 attributes only add tags to the 2nd and beyond, to avoid double collapsing on the first attribute
//                yield! tl |> Seq.collect (fun attr -> rcheck Scope.Attribute Collapse.Same <| Range.startToEnd attr.Range attr.ArgExpr.Range)
//                // visit the expressions inside each attribute
//                yield! attrs |> Seq.collect (fun attr -> parseExpr attr.ArgExpr)
//        }

//    and private parseBinding (Binding (_,kind,_,_,AllAttrs attrs,_,_,_,_,e,br,_) as b) =
//        seq {
////            let r = Range.endToEnd b.RangeOfBindingSansRhs b.RangeOfBindingAndRhs
//            match kind with
//            | SynBindingKind.NormalBinding ->
//                yield! rcheck Scope.LetOrUse Collapse.Below <| Range.endToEnd b.RangeOfBindingSansRhs b.RangeOfBindingAndRhs
//            | SynBindingKind.DoBinding ->
//                yield! rcheck Scope.Do Collapse.Below <| Range.modStart br 2
//            | _ -> ()
//            yield! parseAttributes attrs
//            yield! parseExpr e
//        }

//    and private parseBindings = Seq.collect parseBinding

//    and private parseSynMemberDefn d =
//        seq {
//            match d with
//            | SynMemberDefn.Member (binding, r) ->
//                yield! rcheck Scope.Member Collapse.Below r
//                yield! parseBinding binding
//            | SynMemberDefn.LetBindings (bindings, _, _, _r) ->
//                //yield! rcheck Scope.LetOrUse Collapse.Below r
//                yield! parseBindings bindings
//            | SynMemberDefn.Interface (tp,iMembers,_) ->
//                yield! rcheck Scope.Interface Collapse.Below <| Range.endToEnd tp.Range d.Range
//                match iMembers with
//                | Some members -> yield! Seq.collect parseSynMemberDefn members
//                | None -> ()
//            | SynMemberDefn.NestedType (td, _, _) ->
//                yield! parseTypeDefn td
//            | SynMemberDefn.AbstractSlot (ValSpfn(_, _, _, synt, _, _, _, _, _, _, _), _, r) ->
//                yield! rcheck Scope.Member Collapse.Below <| Range.startToEnd synt.Range r
//            | SynMemberDefn.AutoProperty (_, _, _, _, (*memkind*)_, _, _, _, e, _, r) ->
//                yield! rcheck Scope.Member Collapse.Below r
//                yield! parseExpr e
//            | _ -> ()
//        }

//    (*  For Cases like
//        --------------
//            type JsonDocument =
//                private {   Json : string
//                            Path : string   }
//        Or
//             type JsonDocument =
//                internal |  Json of string
//                         |  Path of string
//    *)
//    and private parseSimpleRepr simple =
//        let _accessRange (opt:SynAccess option) =
//            match opt with
//            | None -> 0
//            | Some synacc ->
//                match synacc with
//                | SynAccess.Public -> 6
//                | SynAccess.Private -> 7
//                | SynAccess.Internal -> 8
//        seq {
//            match simple with
//            | SynTypeDefnSimpleRepr.Enum (cases,er) ->
//                yield! rcheck Scope.SimpleType Collapse.Below er
//                yield!
//                    cases
//                    |> Seq.collect (fun (SynEnumCase.EnumCase (AllAttrs attrs, _, _, _, cr)) ->
//                        seq { yield! rcheck Scope.EnumCase Collapse.Below cr
//                              yield! parseAttributes attrs })
//            | SynTypeDefnSimpleRepr.Record (_opt,fields,rr) ->
//                //yield! rcheck Scope.SimpleType Collapse.Same <| Range.modBoth rr (accessRange opt+1) 1
//                yield! rcheck Scope.RecordDefn Collapse.Same rr //<| Range.modBoth rr 1 1
//                yield! fields
//                    |> Seq.collect (fun (SynField.Field (AllAttrs attrs,_,_,_,_,_,_,fr)) ->
//                    seq{yield! rcheck Scope.RecordField Collapse.Below fr
//                        yield! parseAttributes attrs
//                    })
//            | SynTypeDefnSimpleRepr.Union (_opt,cases,ur) ->
////                yield! rcheck Scope.SimpleType Collapse.Same <| Range.modStart ur (accessRange opt)
//                yield! rcheck Scope.UnionDefn Collapse.Same ur
//                yield! cases
//                    |> Seq.collect (fun (SynUnionCase.UnionCase (AllAttrs attrs,_,_,_,_,cr)) ->
//                    seq{yield! rcheck Scope.UnionCase Collapse.Below cr
//                        yield! parseAttributes attrs
//                    })
//            | _ -> ()
//        }

//    and private parseTypeDefn (TypeDefn (componentInfo, objectModel, members, range)) =
//        seq {
//            match objectModel with
//            | SynTypeDefnRepr.ObjectModel (defnKind, objMembers, _) ->
//                match defnKind with
//                | SynTypeDefnKind.TyconAugmentation ->
//                    yield! rcheck Scope.TypeExtension Collapse.Below <| Range.endToEnd componentInfo.Range range
//                | _ ->
//                    yield! rcheck Scope.Type Collapse.Below <| Range.endToEnd componentInfo.Range range
//                yield! Seq.collect parseSynMemberDefn objMembers
//                // visit the members of a type extension
//                yield! Seq.collect parseSynMemberDefn members
//            | SynTypeDefnRepr.Simple (simpleRepr,_r) ->
//                yield! rcheck Scope.Type Collapse.Below <| Range.endToEnd componentInfo.Range range
//                yield! parseSimpleRepr simpleRepr
//                yield! Seq.collect parseSynMemberDefn members
//            | SynTypeDefnRepr.Exception _ -> ()
//        }

//    let private getConsecutiveModuleDecls (predicate: SynModuleDecl -> range option) (scope:Scope) (decls: SynModuleDecl list) =
//        let groupConsecutiveDecls input =
//            let rec loop (input: range list) (res: range list list) currentBulk =
//                match input, currentBulk with
//                | [], [] -> List.rev res
//                | [], _ -> List.rev (currentBulk::res)
//                | r :: rest, [] -> loop rest res [r]
//                | r :: rest, last :: _ when r.StartLine = last.EndLine + 1 ->
//                    loop rest res (r::currentBulk)
//                | r :: rest, _ -> loop rest (currentBulk::res) [r]
//            loop input [] []

//        let selectRanges (ranges: range list) =
//            match ranges with
//            | [] -> None
//            | [r] when r.StartLine = r.EndLine -> None
//            | [r] -> Some <| ScopeRange (scope, Collapse.Same, (Range.mkRange "" r.Start r.End))
//            | lastRange :: rest ->
//                let firstRange = Seq.last rest
//                Some <| ScopeRange (scope, Collapse.Same, (Range.mkRange "" firstRange.Start lastRange.End))

//        decls |> (List.choose predicate >> groupConsecutiveDecls >> List.choose selectRanges)


//    let collectOpens = getConsecutiveModuleDecls (function SynModuleDecl.Open (_, r) -> Some r | _ -> None) Scope.Open

//    let collectHashDirectives =
//         getConsecutiveModuleDecls(
//            function
//            | SynModuleDecl.HashDirective (ParsedHashDirective (directive, _, _),r) ->
//                let prefixLength = "#".Length + directive.Length + " ".Length
//                Some (Range.mkRange "" (Range.mkPos r.StartLine prefixLength) r.End)
//            | _ -> None) Scope.HashDirective


//    let rec private parseDeclaration (decl: SynModuleDecl) =
//        seq {
//            match decl with
//            | SynModuleDecl.Let (_,bindings,_) ->
//                yield! parseBindings bindings
//            | SynModuleDecl.Types (types,_) ->
//                yield! Seq.collect parseTypeDefn types
//            // Fold the attributes above a module
//            | SynModuleDecl.NestedModule (SynComponentInfo.ComponentInfo (AllAttrs attrs,_,_,_,_,_,_,cmpRange),_, decls,_,_) ->
//                // Outline the full scope of the module
//                yield! rcheck Scope.Module Collapse.Below <| Range.endToEnd cmpRange decl.Range
//                // A module's component info stores the ranges of its attributes
//                yield! parseAttributes attrs
//                yield! collectOpens decls
//                yield! Seq.collect parseDeclaration decls
//            | SynModuleDecl.DoExpr (_,e,_) ->
//                yield! parseExpr e
//            | SynModuleDecl.Attributes (AllAttrs attrs,_) ->
//                yield! parseAttributes attrs
//            | _ -> ()
//        }

//    let private parseModuleOrNamespace moduleOrNs =
//        seq { let (SynModuleOrNamespace.SynModuleOrNamespace (_,_,_,decls,_,_,_,_)) = moduleOrNs
//              yield! collectHashDirectives decls
//              yield! collectOpens decls
//              yield! Seq.collect parseDeclaration decls }

//    type private LineNum = int
//    type private LineStr = string
//    type private CommentType = Regular | XmlDoc

//    [<NoComparison>]
//    type private CommentList =
//        { Lines: ResizeArray<LineNum * LineStr>
//          Type: CommentType }
//        static member New ty lineStr =
//            { Type = ty; Lines = ResizeArray [| lineStr |] }

//    let private (|Comment|_|) line =
//        match line with
//        | String.StartsWith "///" -> Some XmlDoc
//        | String.StartsWith "//" -> Some Regular
//        | _ -> None

//    let getCommentRanges (lines: string[]) =
//        let comments: CommentList list =
//            lines
//            |> Array.foldi (fun ((lastLineNum, currentComment: CommentList option, result) as state) lineNum lineStr ->
//                match lineStr.TrimStart(), currentComment with
//                | Comment commentType, Some comment ->
//                    if comment.Type = commentType && lineNum = lastLineNum + 1 then
//                        comment.Lines.Add (lineNum, lineStr)
//                        lineNum, currentComment, result
//                    else lineNum, Some (CommentList.New commentType (lineNum, lineStr)), comment :: result
//                | Comment commentType, None ->
//                    lineNum, Some (CommentList.New commentType (lineNum, lineStr)), result
//                | _, Some comment ->
//                    lineNum, None, comment :: result
//                | _ -> state)
//               (-1, None, [])
//            |> fun (_, lastComment, comments) ->
//                match lastComment with
//                | Some comment ->
//                    comment :: comments
//                | _ -> comments
//                |> List.rev

//        comments
//        |> List.filter (fun comment -> comment.Lines.Count > 1)
//        |> List.map (fun comment ->
//            let lines = comment.Lines
//            let startLine, startStr = lines.[0]
//            let endLine, endStr = lines.[lines.Count - 1]
//            let startCol = startStr.IndexOf '/'
//            let endCol = endStr.TrimEnd().Length

//            let scopeType =
//                match comment.Type with
//                | Regular -> Scope.Comment
//                | XmlDoc -> Scope.XmlDocComment
//            ScopeRange(
//                scopeType,
//                Collapse.Same,
//                Range.mkRange
//                    ""
//                    (Range.mkPos (startLine + 1) startCol)
//                    (Range.mkPos (endLine + 1) endCol)))

//    let getOutliningRanges sourceLines tree =
//        match tree with
//        | ParsedInput.ImplFile implFile ->
//            let (ParsedImplFileInput (_, _, _, _, _, modules, _)) = implFile
//            let astBasedRanges = Seq.collect parseModuleOrNamespace modules
//            let commentRanges = getCommentRanges sourceLines
//            Seq.append astBasedRanges commentRanges
//        | _ -> Seq.empty

//module Printf =
//    [<NoComparison>]
//    type PrintfFunction =
//        { FormatString: Range.range
//          Args: Range.range[] }

//    [<NoComparison>]
//    type private AppWithArg =
//        { Range: Range.range
//          Arg: Range.range }

//    let internal getAll (input: ParsedInput option) : PrintfFunction[] =
//        let result = ResizeArray()
//        let appStack: AppWithArg list ref = ref []

//        let addAppWithArg appWithArg =
//            match !appStack with
//            | lastApp :: _ when not (Range.rangeContainsRange lastApp.Range appWithArg.Range) ->
//                appStack := [appWithArg]
//            | _ -> appStack := appWithArg :: !appStack

//        let rec walkImplFileInput (ParsedImplFileInput(_, _, _, _, _, moduleOrNamespaceList, _)) =
//            List.iter walkSynModuleOrNamespace moduleOrNamespaceList

//        and walkSynModuleOrNamespace (SynModuleOrNamespace(_, _, _, decls, _, _, _, _)) =
//            List.iter walkSynModuleDecl decls

//        and walkTypeConstraint = function
//            | SynTypeConstraint.WhereTyparDefaultsToType (_, ty, _)
//            | SynTypeConstraint.WhereTyparSubtypeOfType (_, ty, _) -> walkType ty
//            | SynTypeConstraint.WhereTyparIsEnum (_, ts, _)
//            | SynTypeConstraint.WhereTyparIsDelegate (_, ts, _) -> List.iter walkType ts
//            | SynTypeConstraint.WhereTyparSupportsMember (_, sign, _) -> walkMemberSig sign
//            | _ -> ()

//        and walkBinding (SynBinding.Binding (_, _, _, _, _, _, _, _, returnInfo, e, _, _)) =
//            walkExpr e
//            returnInfo |> Option.iter (fun (SynBindingReturnInfo (t, _, _)) -> walkType t)

//        and walkInterfaceImpl (InterfaceImpl(_, bindings, _)) = List.iter walkBinding bindings

//        and walkIndexerArg = function
//            | SynIndexerArg.One(e,_fromEnd,_range) -> walkExpr e
//            | SynIndexerArg.Two(e1,_e1FromEnd,e2,_e2FromEnd,_e1Range,_e2Range) -> List.iter walkExpr [e1; e2]

//        and walkType = function
//            | SynType.Array (_, t, _)
//            | SynType.HashConstraint (t, _)
//            | SynType.MeasurePower (t, _, _) -> walkType t
//            | SynType.Fun (t1, t2, _)
//            | SynType.MeasureDivide (t1, t2, _) -> walkType t1; walkType t2
//            | SynType.App (ty, _, types, _, _, _, _) -> walkType ty; List.iter walkType types
//            | SynType.LongIdentApp (_, _, _, types, _, _, _) -> List.iter walkType types
//            | SynType.Tuple (_, ts, _) -> ts |> List.iter (fun (_, t) -> walkType t)
//            | SynType.WithGlobalConstraints (t, typeConstraints, _) ->
//                walkType t; List.iter walkTypeConstraint typeConstraints
//            | _ -> ()

//        and walkClause (Clause (_, e1, e2, _, _)) =
//            walkExpr e2
//            e1 |> Option.iter walkExpr

//        and walkSimplePats = function
//            | SynSimplePats.SimplePats (pats, _) -> List.iter walkSimplePat pats
//            | SynSimplePats.Typed (pats, ty, _) ->
//                walkSimplePats pats
//                walkType ty

//        and walkExpr e =
//            match e with
//            | SynExpr.App (_, _, SynExpr.Ident _, SynExpr.Const (SynConst.String (_, stringRange), _), r) ->
//                match !appStack with
//                | (lastApp :: _) as apps when Range.rangeContainsRange lastApp.Range e.Range ->
//                    let intersectsWithFuncOrString (arg: Range.range) =
//                        Range.rangeContainsRange arg stringRange
//                        || arg = stringRange
//                        || Range.rangeContainsRange arg r
//                        || arg = r

//                    let rec loop acc (apps: AppWithArg list) =
//                        match acc, apps with
//                        | _, [] -> acc
//                        | [], h :: t ->
//                            if not (intersectsWithFuncOrString h.Arg) then
//                                loop [h] t
//                            else loop [] t
//                        | prev :: _, curr :: rest ->
//                            if Range.rangeContainsRange curr.Range prev.Range
//                               && not (intersectsWithFuncOrString curr.Arg) then
//                                loop (curr :: acc) rest
//                            else acc

//                    let args =
//                        apps
//                        |> loop []
//                        |> List.rev
//                        |> List.map (fun x -> x.Arg)
//                        |> List.toArray
//                    let res = { FormatString = stringRange
//                                Args = args }
//                    result.Add res
//                | _ -> ()
//                appStack := []
//            | SynExpr.App (_, _, SynExpr.App(_, true, SynExpr.Ident op, e1, _), e2, _) ->
//                let rec deconstruct = function
//                    | SynExpr.Paren (exp, _, _, _) -> deconstruct exp
//                    | SynExpr.Tuple (_, exps, _, _) ->
//                        exps |> List.iter (fun exp -> addAppWithArg { Range = e.Range; Arg = exp.Range})
//                        ()
//                    | _ -> ()

//                addAppWithArg { Range = e.Range; Arg = e2.Range }
//                if op.idText = (PrettyNaming.CompileOpName "||>")
//                        || op.idText = (PrettyNaming.CompileOpName "|||>") then
//                    deconstruct e1
//                    walkExpr e2
//                else
//                    if op.idText = (PrettyNaming.CompileOpName "|>") then
//                        addAppWithArg { Range = e.Range; Arg = e1.Range }
//                    walkExpr e2
//                    walkExpr e1
//            | SynExpr.App (_, _, SynExpr.App(_, true, _, e1, _), e2, _) ->
//                addAppWithArg { Range = e.Range; Arg = e2.Range }
//                addAppWithArg { Range = e.Range; Arg = e1.Range }
//                walkExpr e1
//                walkExpr e2
//            | SynExpr.App (_, _, e1, e2, _) ->
//                addAppWithArg { Range = e.Range; Arg = e2.Range }
//                walkExpr e1
//                walkExpr e2
//            | _ ->
//                match e with
//                | SynExpr.Paren (e, _, _, _)
//                | SynExpr.Quote (_, _, e, _, _)
//                | SynExpr.Typed (e, _, _)
//                | SynExpr.InferredUpcast (e, _)
//                | SynExpr.InferredDowncast (e, _)
//                | SynExpr.AddressOf (_, e, _, _)
//                | SynExpr.DoBang (e, _)
//                | SynExpr.YieldOrReturn (_, e, _)
//                | SynExpr.ArrayOrListOfSeqExpr (_, e, _)
//                | SynExpr.CompExpr (_, _, e, _)
//                | SynExpr.Do (e, _)
//                | SynExpr.Assert (e, _)
//                | SynExpr.Lazy (e, _)
//                | SynExpr.YieldOrReturnFrom (_, e, _) -> walkExpr e
//                | SynExpr.Lambda (_, _, pats, e, _) ->
//                    walkSimplePats pats
//                    walkExpr e
//                | SynExpr.New (_, t, e, _)
//                | SynExpr.TypeTest (e, t, _)
//                | SynExpr.Upcast (e, t, _)
//                | SynExpr.Downcast (e, t, _) -> walkExpr e; walkType t
//                | SynExpr.Tuple (_, es, _, _)
//                | Sequentials es
//                | SynExpr.ArrayOrList (_, es, _) -> List.iter walkExpr es
//                | SynExpr.TryFinally (e1, e2, _, _, _)
//                | SynExpr.While (_, e1, e2, _) -> List.iter walkExpr [e1; e2]
//                | SynExpr.Record (_, _, fields, _) ->
//                    fields |> List.iter (fun (_, e, _) -> e |> Option.iter walkExpr)
//                | SynExpr.ObjExpr(ty, argOpt, bindings, ifaces, _, _) ->
//                    argOpt |> Option.iter (fun (e, _) -> walkExpr e)
//                    walkType ty
//                    List.iter walkBinding bindings
//                    List.iter walkInterfaceImpl ifaces
//                | SynExpr.For (_, _, e1, _, e2, e3, _) -> List.iter walkExpr [e1; e2; e3]
//                | SynExpr.ForEach (_, _, _, _, e1, e2, _) -> List.iter walkExpr [e1; e2]
//                | SynExpr.MatchLambda (_, _, synMatchClauseList, _, _) ->
//                    List.iter walkClause synMatchClauseList
//                | SynExpr.Match (_, e, synMatchClauseList, _) ->
//                    walkExpr e
//                    List.iter walkClause synMatchClauseList
//                | SynExpr.TypeApp (e, _, tys, _, _, _, _) ->
//                    List.iter walkType tys; walkExpr e
//                | SynExpr.LetOrUse (_, _, bindings, e, _) ->
//                    List.iter walkBinding bindings; walkExpr e
//                | SynExpr.TryWith (e, _, clauses, _, _, _, _) ->
//                    List.iter walkClause clauses;  walkExpr e
//                | SynExpr.IfThenElse (e1, e2, e3, _, _, _, _) ->
//                    List.iter walkExpr [e1; e2]
//                    e3 |> Option.iter walkExpr
//                | SynExpr.LongIdentSet (_, e, _)
//                | SynExpr.DotGet (e, _, _, _) -> walkExpr e
//                | SynExpr.DotSet (e1, _, e2, _) ->
//                    walkExpr e1
//                    walkExpr e2
//                | SynExpr.DotIndexedGet (e, args, _, _) ->
//                    walkExpr e
//                    List.iter walkIndexerArg args
//                | SynExpr.DotIndexedSet (e1, args, e2, _, _, _) ->
//                    walkExpr e1
//                    List.iter walkIndexerArg args
//                    walkExpr e2
//                | SynExpr.NamedIndexedPropertySet (_, e1, e2, _) -> List.iter walkExpr [e1; e2]
//                | SynExpr.DotNamedIndexedPropertySet (e1, _, e2, e3, _) -> List.iter walkExpr [e1; e2; e3]
//                | SynExpr.JoinIn (e1, _, e2, _) -> List.iter walkExpr [e1; e2]
//                | SynExpr.LetOrUseBang (_, _, _, _, e1, ands, e2, _) ->
//                  walkExpr e1
//                  for (_,_,_,_,body,_) in ands do
//                    walkExpr body
//                  walkExpr e2
//                | SynExpr.TraitCall (_, sign, e, _) ->
//                    walkMemberSig sign
//                    walkExpr e
//                | SynExpr.Const (SynConst.Measure(_, m), _) -> walkMeasure m
//                | _ -> ()

//        and walkMeasure = function
//            | SynMeasure.Product (m1, m2, _)
//            | SynMeasure.Divide (m1, m2, _) -> walkMeasure m1; walkMeasure m2
//            | SynMeasure.Seq (ms, _) -> List.iter walkMeasure ms
//            | SynMeasure.Power (m, _, _) -> walkMeasure m
//            | SynMeasure.One
//            | SynMeasure.Anon _
//            | SynMeasure.Named _
//            | SynMeasure.Var _ -> ()

//        and walkSimplePat = function
//            | SynSimplePat.Attrib (pat, _, _) -> walkSimplePat pat
//            | SynSimplePat.Typed(_, t, _) -> walkType t
//            | _ -> ()

//        and walkField (SynField.Field(_, _, _, t, _, _, _, _)) = walkType t

//        and walkMemberSig = function
//            | SynMemberSig.Inherit (t, _)
//            | SynMemberSig.Interface(t, _) -> walkType t
//            | SynMemberSig.ValField(f, _) -> walkField f
//            | SynMemberSig.NestedType(SynTypeDefnSig.TypeDefnSig (_, repr, memberSigs, _), _) ->
//                walkTypeDefnSigRepr repr
//                List.iter walkMemberSig memberSigs
//            | SynMemberSig.Member _ -> ()

//        and walkMember = function
//            | SynMemberDefn.Member (binding, _) -> walkBinding binding
//            | SynMemberDefn.ImplicitCtor (_, _, AllSimplePats pats, _, _) -> List.iter walkSimplePat pats
//            | SynMemberDefn.ImplicitInherit (t, e, _, _) -> walkType t; walkExpr e
//            | SynMemberDefn.LetBindings (bindings, _, _, _) -> List.iter walkBinding bindings
//            | SynMemberDefn.Interface (t, members, _) ->
//                walkType t
//                members |> Option.iter (List.iter walkMember)
//            | SynMemberDefn.Inherit (t, _, _) -> walkType t
//            | SynMemberDefn.ValField (field, _) -> walkField field
//            | SynMemberDefn.NestedType (tdef, _, _) -> walkTypeDefn tdef
//            | SynMemberDefn.AutoProperty (_, _, _, t, _, _, _, _, e, _, _) ->
//                Option.iter walkType t
//                walkExpr e
//            | _ -> ()

//        and walkTypeDefnRepr = function
//            | SynTypeDefnRepr.ObjectModel (_, defns, _) -> List.iter walkMember defns
//            | SynTypeDefnRepr.Simple _ -> ()
//            | SynTypeDefnRepr.Exception _ -> ()

//        and walkTypeDefnSigRepr = function
//            | SynTypeDefnSigRepr.ObjectModel (_, defns, _) -> List.iter walkMemberSig defns
//            | SynTypeDefnSigRepr.Simple _ -> ()
//            | SynTypeDefnSigRepr.Exception _ -> ()

//        and walkTypeDefn (TypeDefn (_, repr, members, _)) =
//            walkTypeDefnRepr repr
//            List.iter walkMember members

//        and walkSynModuleDecl (decl: SynModuleDecl) =
//            match decl with
//            | SynModuleDecl.NamespaceFragment fragment -> walkSynModuleOrNamespace fragment
//            | SynModuleDecl.NestedModule (_, _, modules, _, _) ->
//                List.iter walkSynModuleDecl modules
//            | SynModuleDecl.Let (_, bindings, _) -> List.iter walkBinding bindings
//            | SynModuleDecl.DoExpr (_, expr, _) -> walkExpr expr
//            | SynModuleDecl.Types (types, _) -> List.iter walkTypeDefn types
//            | _ -> ()

//        match input with
//        | Some (ParsedInput.ImplFile input) ->
//             walkImplFileInput input
//        | _ -> ()
//        //debug "%A" idents
//        result.ToArray()
