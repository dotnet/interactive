namespace FSharp.Compiler

module Syntax =
  open FSharp.Compiler.Syntax

  /// A pattern that collects all attributes from a `SynAttributes` into a single flat list
  let (|AllAttrs|) (attrs: SynAttributes) = attrs |> List.collect (fun attrList -> attrList.Attributes)

  /// An recursive pattern that collect all sequential expressions to avoid StackOverflowException
  let rec (|Sequentials|_|) =
    function
    | SynExpr.Sequential(expr1 = e; expr2 = Sequentials es) -> Some(e :: es)
    | SynExpr.Sequential(expr1 = e1; expr2 = e2) -> Some [ e1; e2 ]
    | _ -> None

  let (|ConstructorPats|) =
    function
    | SynArgPats.Pats ps -> ps
    | SynArgPats.NamePatPairs(pats = xs) -> xs |> List.map (fun (_, _, pat) -> pat)

  /// A pattern that collects all patterns from a `SynSimplePats` into a single flat list
  let (|AllSimplePats|) (pats: SynSimplePats) =
    let rec loop acc pat =
      match pat with
      | SynSimplePats.SimplePats(pats = pats) -> acc @ pats

    loop [] pats

  type SyntaxCollectorBase() =
    abstract WalkSynModuleOrNamespace: SynModuleOrNamespace -> unit
    default _.WalkSynModuleOrNamespace _ = ()
    abstract WalkAttribute: SynAttribute -> unit
    default _.WalkAttribute _ = ()
    abstract WalkSynModuleDecl: SynModuleDecl -> unit
    default _.WalkSynModuleDecl _ = ()
    abstract WalkExpr: SynExpr -> unit
    default _.WalkExpr _ = ()
    abstract WalkTypar: SynTypar -> unit
    default _.WalkTypar _ = ()
    abstract WalkTyparDecl: SynTyparDecl -> unit
    default _.WalkTyparDecl _ = ()
    abstract WalkTypeConstraint: SynTypeConstraint -> unit
    default _.WalkTypeConstraint _ = ()
    abstract WalkType: SynType -> unit
    default _.WalkType _ = ()
    abstract WalkMemberSig: SynMemberSig -> unit
    default _.WalkMemberSig _ = ()
    abstract WalkPat: SynPat -> unit
    default _.WalkPat _ = ()
    abstract WalkValTyparDecls: SynValTyparDecls -> unit
    default _.WalkValTyparDecls _ = ()
    abstract WalkBinding: SynBinding -> unit
    default _.WalkBinding _ = ()
    abstract WalkSimplePat: SynSimplePat -> unit
    default _.WalkSimplePat _ = ()
    abstract WalkInterfaceImpl: SynInterfaceImpl -> unit
    default _.WalkInterfaceImpl _ = ()
    abstract WalkClause: SynMatchClause -> unit
    default _.WalkClause _ = ()
    abstract WalkInterpolatedStringPart: SynInterpolatedStringPart -> unit
    default _.WalkInterpolatedStringPart _ = ()
    abstract WalkMeasure: SynMeasure -> unit
    default _.WalkMeasure _ = ()
    abstract WalkComponentInfo: SynComponentInfo -> unit
    default _.WalkComponentInfo _ = ()
    abstract WalkTypeDefnSigRepr: SynTypeDefnSigRepr -> unit
    default _.WalkTypeDefnSigRepr _ = ()
    abstract WalkUnionCaseType: SynUnionCaseKind -> unit
    default _.WalkUnionCaseType _ = ()
    abstract WalkEnumCase: SynEnumCase -> unit
    default _.WalkEnumCase _ = ()
    abstract WalkField: SynField -> unit
    default _.WalkField _ = ()
    abstract WalkTypeDefnSimple: SynTypeDefnSimpleRepr -> unit
    default _.WalkTypeDefnSimple _ = ()
    abstract WalkValSig: SynValSig -> unit
    default _.WalkValSig _ = ()
    abstract WalkMember: SynMemberDefn -> unit
    default _.WalkMember _ = ()
    abstract WalkUnionCase: SynUnionCase -> unit
    default _.WalkUnionCase _ = ()
    abstract WalkTypeDefnRepr: SynTypeDefnRepr -> unit
    default _.WalkTypeDefnRepr _ = ()
    abstract WalkTypeDefn: SynTypeDefn -> unit
    default _.WalkTypeDefn _ = ()

  let walkAst (walker: SyntaxCollectorBase) (input: ParsedInput) : unit =

    let rec walkImplFileInput (ParsedImplFileInput(contents = moduleOrNamespaceList)) =
      List.iter walkSynModuleOrNamespace moduleOrNamespaceList
      ()

    and walkSynModuleOrNamespace (SynModuleOrNamespace(decls = decls; attribs = AllAttrs attrs; range = _) as s) =
      walker.WalkSynModuleOrNamespace s
      List.iter walkAttribute attrs
      List.iter walkSynModuleDecl decls

    and walkAttribute (attr: SynAttribute) = walkExpr attr.ArgExpr

    and walkTyparDecl (SynTyparDecl(attributes = AllAttrs attrs; typar = typar; intersectionConstraints = ts)) =
      List.iter walkAttribute attrs
      walkTypar typar
      List.iter walkType ts

    and walkTyparDecls (typars: SynTyparDecls) =
      typars.TyparDecls |> List.iter walkTyparDecl
      typars.Constraints |> List.iter walkTypeConstraint

    and walkSynValTyparDecls (SynValTyparDecls(typars, _)) = Option.iter walkTyparDecls typars

    and walkTypeConstraint s =
      walker.WalkTypeConstraint s

      match s with
      | SynTypeConstraint.WhereTyparIsValueType(t, _)
      | SynTypeConstraint.WhereTyparIsReferenceType(t, _)
      | SynTypeConstraint.WhereTyparIsUnmanaged(t, _)
      | SynTypeConstraint.WhereTyparSupportsNull(t, _)
      | SynTypeConstraint.WhereTyparIsComparable(t, _)
      | SynTypeConstraint.WhereTyparIsEquatable(t, _) -> walkTypar t
      | SynTypeConstraint.WhereTyparDefaultsToType(t, ty, _)
      | SynTypeConstraint.WhereTyparSubtypeOfType(t, ty, _) ->
        walkTypar t
        walkType ty
      | SynTypeConstraint.WhereTyparIsEnum(t, ts, _)
      | SynTypeConstraint.WhereTyparIsDelegate(t, ts, _) ->
        walkTypar t
        List.iter walkType ts
      | SynTypeConstraint.WhereTyparSupportsMember(t, sign, _) ->
        walkType t
        walkMemberSig sign
      | SynTypeConstraint.WhereSelfConstrained(t, _) -> walkType t
      | SynTypeConstraint.WhereTyparNotSupportsNull(t, _, _) -> walkTypar t

    and walkPat s =
      walker.WalkPat s

      match s with
      | SynPat.Tuple(elementPats = pats)
      | SynPat.ArrayOrList(elementPats = pats)
      | SynPat.Ands(pats = pats) -> List.iter walkPat pats
      | SynPat.Named _ -> ()
      | SynPat.Typed(pat, t, _) ->
        walkPat pat
        walkType t
      | SynPat.Attrib(pat, AllAttrs attrs, _) ->
        walkPat pat
        List.iter walkAttribute attrs
      | SynPat.Or(pat1, pat2, _, _) -> List.iter walkPat [ pat1; pat2 ]
      | SynPat.LongIdent(typarDecls = typars; argPats = ConstructorPats pats; range = _) ->
        Option.iter walkSynValTyparDecls typars
        List.iter walkPat pats
      | SynPat.Paren(pat, _) -> walkPat pat
      | SynPat.IsInst(t, _) -> walkType t
      | SynPat.QuoteExpr(e, _) -> walkExpr e
      | SynPat.Const _ -> ()
      | SynPat.Wild _ -> ()
      | SynPat.Record _ -> ()
      | SynPat.Null _ -> ()
      | SynPat.OptionalVal _ -> ()
      | SynPat.InstanceMember _ -> ()
      | SynPat.FromParseError _ -> ()
      | SynPat.As(lpat, rpat, _) ->
        walkPat lpat
        walkPat rpat
      | SynPat.ListCons(lpat, rpat, _, _) ->
        walkPat lpat
        walkPat rpat

    and walkTypar (SynTypar _ as s) = walker.WalkTypar s

    and walkBinding
      (SynBinding(attributes = AllAttrs attrs; headPat = pat; returnInfo = returnInfo; expr = e; range = _) as s)
      =
      walker.WalkBinding s
      List.iter walkAttribute attrs
      walkPat pat
      walkExpr e

      returnInfo
      |> Option.iter (fun (SynBindingReturnInfo(t, _, attrs, _)) ->
        walkType t
        walkAttributes attrs)

    and walkAttributes (attrs: SynAttributes) =
      List.iter (fun (attrList: SynAttributeList) -> List.iter walkAttribute attrList.Attributes) attrs

    and walkInterfaceImpl (SynInterfaceImpl(bindings = bindings; range = _) as s) =
      walker.WalkInterfaceImpl s
      List.iter walkBinding bindings

    and walkType s =
      walker.WalkType s

      match s with
      | SynType.Array(_, t, _)
      | SynType.HashConstraint(t, _)
      | SynType.MeasurePower(t, _, _) -> walkType t
      | SynType.Fun(t1, t2, _, _) ->
        // | SynType.MeasureDivide(t1, t2, r) ->
        walkType t1
        walkType t2
      | SynType.App(ty, _, types, _, _, _, _) ->
        walkType ty
        List.iter walkType types
      | SynType.LongIdentApp(_, _, _, types, _, _, _) -> List.iter walkType types
      | SynType.Tuple(_, ts, _) ->
        ts
        |> List.iter (function
          | SynTupleTypeSegment.Type t -> walkType t
          | _ -> ())
      | SynType.WithGlobalConstraints(t, typeConstraints, _) ->
        walkType t
        List.iter walkTypeConstraint typeConstraints
      | SynType.LongIdent _ -> ()
      | SynType.AnonRecd _ -> ()
      | SynType.Var _ -> ()
      | SynType.Anon _ -> ()
      | SynType.StaticConstant _ -> ()
      | SynType.StaticConstantExpr _ -> ()
      | SynType.StaticConstantNamed _ -> ()
      | SynType.Paren(innerType, _) -> walkType innerType
      | SynType.SignatureParameter(usedType = t; range = _) -> walkType t
      | SynType.Or(lhs, rhs, _, _) ->
        walkType lhs
        walkType rhs
      | SynType.FromParseError _ -> ()
      | SynType.Intersection(typar, types, _, _) ->
        Option.iter walkTypar typar
        List.iter walkType types
      | SynType.StaticConstantNull(_) -> ()
      | SynType.WithNull(t, _, _, _) -> walkType t

    and walkClause (SynMatchClause(pat, e1, e2, _, _, _) as s) =
      walker.WalkClause s
      walkPat pat
      walkExpr e2
      e1 |> Option.iter walkExpr

    and walkSimplePats =
      function
      | SynSimplePats.SimplePats(pats = pats; range = _) -> List.iter walkSimplePat pats

    and walkInterpolatedStringPart s =
      walker.WalkInterpolatedStringPart s

      match s with
      | SynInterpolatedStringPart.FillExpr(expr, _) -> walkExpr expr
      | SynInterpolatedStringPart.String _ -> ()

    and walkExpr s =
      walker.WalkExpr s

      match s with
      | SynExpr.Typed(expr = e)
      | SynExpr.Paren(expr = e)
      | SynExpr.InferredUpcast(expr = e)
      | SynExpr.InferredDowncast(expr = e)
      | SynExpr.AddressOf(expr = e)
      | SynExpr.DoBang(expr = e)
      | SynExpr.YieldOrReturn(expr = e)
      | SynExpr.ArrayOrListComputed(expr = e)
      | SynExpr.ComputationExpr(expr = e)
      | SynExpr.Do(expr = e)
      | SynExpr.Assert(expr = e)
      | SynExpr.Lazy(expr = e)
      | SynExpr.YieldOrReturnFrom(expr = e)
      | SynExpr.DotLambda(expr = e) -> walkExpr e
      | SynExpr.Quote(operator, _, quotedExpr, _, _) ->
        walkExpr operator
        walkExpr quotedExpr
      | SynExpr.SequentialOrImplicitYield(_, e1, e2, ifNotE, _) ->
        walkExpr e1
        walkExpr e2
        walkExpr ifNotE
      | SynExpr.Lambda(args = pats; body = e; range = _) ->
        walkSimplePats pats
        walkExpr e
      | SynExpr.New(_, t, e, _)
      | SynExpr.TypeTest(e, t, _)
      | SynExpr.Upcast(e, t, _)
      | SynExpr.Downcast(e, t, _) ->
        walkExpr e
        walkType t
      | SynExpr.Tuple(_, es, _, _)
      | Sequentials es -> List.iter walkExpr es //TODO??
      | SynExpr.ArrayOrList(_, es, _) -> List.iter walkExpr es
      | SynExpr.App(_, _, e1, e2, _)
      | SynExpr.TryFinally(e1, e2, _, _, _, _)
      | SynExpr.While(_, e1, e2, _) -> List.iter walkExpr [ e1; e2 ]
      | SynExpr.Record(_, _, fields, _) ->

        fields
        |> List.iter (fun (SynExprRecordField(fieldName = (_, _); expr = e)) -> e |> Option.iter walkExpr)
      | SynExpr.ObjExpr(ty, argOpt, _, bindings, _, ifaces, _, _) ->

        argOpt |> Option.iter (fun (e, _) -> walkExpr e)

        walkType ty
        List.iter walkBinding bindings
        List.iter walkInterfaceImpl ifaces
      | SynExpr.For(identBody = e1; toBody = e2; doBody = e3; range = _) -> List.iter walkExpr [ e1; e2; e3 ]
      | SynExpr.ForEach(_, _, _, _, pat, e1, e2, _) ->
        walkPat pat
        List.iter walkExpr [ e1; e2 ]
      | SynExpr.MatchLambda(_, _, synMatchClauseList, _, _) -> List.iter walkClause synMatchClauseList
      | SynExpr.Match(expr = e; clauses = synMatchClauseList; range = _) ->
        walkExpr e
        List.iter walkClause synMatchClauseList
      | SynExpr.TypeApp(e, _, tys, _, _, _, _) ->
        List.iter walkType tys
        walkExpr e
      | SynExpr.LetOrUse(bindings = bindings; body = e; range = _) ->
        List.iter walkBinding bindings
        walkExpr e
      | SynExpr.TryWith(tryExpr = e; withCases = clauses; range = _) ->
        List.iter walkClause clauses
        walkExpr e
      | SynExpr.IfThenElse(ifExpr = e1; thenExpr = e2; elseExpr = e3; range = _) ->
        List.iter walkExpr [ e1; e2 ]
        e3 |> Option.iter walkExpr
      | SynExpr.LongIdentSet(_, e, _)
      | SynExpr.DotGet(e, _, _, _) -> walkExpr e
      | SynExpr.DotSet(e1, _, e2, _) ->
        walkExpr e1
        walkExpr e2
      | SynExpr.DotIndexedGet(e, args, _, _) ->
        walkExpr e
        walkExpr args
      | SynExpr.DotIndexedSet(e1, args, e2, _, _, _) ->
        walkExpr e1
        walkExpr args
        walkExpr e2
      | SynExpr.NamedIndexedPropertySet(_, e1, e2, _) -> List.iter walkExpr [ e1; e2 ]
      | SynExpr.DotNamedIndexedPropertySet(e1, _, e2, e3, _) -> List.iter walkExpr [ e1; e2; e3 ]
      | SynExpr.JoinIn(e1, _, e2, _) -> List.iter walkExpr [ e1; e2 ]
      | SynExpr.LetOrUseBang(pat = pat; rhs = e1; andBangs = ands; body = e2; range = _) ->
        walkPat pat
        walkExpr e1

        for SynExprAndBang(pat = pat; body = body; range = _) in ands do
          walkPat pat
          walkExpr body

        walkExpr e2
      | SynExpr.TraitCall(t, sign, e, _) ->
        walkType t
        walkMemberSig sign
        walkExpr e
      | SynExpr.Const(SynConst.Measure(synMeasure = m), _) -> walkMeasure m
      | SynExpr.Const _ -> ()
      | SynExpr.AnonRecd _ -> ()
      | SynExpr.Sequential _ -> ()
      | SynExpr.Ident _ -> ()
      | SynExpr.LongIdent _ -> ()
      | SynExpr.Set _ -> ()
      | SynExpr.Null _ -> ()
      | SynExpr.ImplicitZero _ -> ()
      | SynExpr.MatchBang(range = _) -> ()
      | SynExpr.LibraryOnlyILAssembly _ -> ()
      | SynExpr.LibraryOnlyStaticOptimization _ -> ()
      | SynExpr.LibraryOnlyUnionCaseFieldGet _ -> ()
      | SynExpr.LibraryOnlyUnionCaseFieldSet _ -> ()
      | SynExpr.ArbitraryAfterError _ -> ()
      | SynExpr.FromParseError _ -> ()
      | SynExpr.DiscardAfterMissingQualificationAfterDot _ -> ()
      | SynExpr.Fixed _ -> ()
      | SynExpr.InterpolatedString(parts, _, _) ->

        for part in parts do
          walkInterpolatedStringPart part
      | SynExpr.IndexFromEnd(itemExpr, _) -> walkExpr itemExpr
      | SynExpr.IndexRange(e1, _, e2, _, _, _) ->
        Option.iter walkExpr e1
        Option.iter walkExpr e2
      | SynExpr.DebugPoint(innerExpr = expr) -> walkExpr expr
      | SynExpr.Dynamic(funcExpr = e1; argExpr = e2; range = _) ->
        walkExpr e1
        walkExpr e2
      | SynExpr.Typar(t, _) -> walkTypar t
      | SynExpr.WhileBang(whileExpr = whileExpr; doExpr = doExpr) ->
        walkExpr whileExpr
        walkExpr doExpr

    and walkMeasure s =
      walker.WalkMeasure s

      match s with
      | SynMeasure.Product(measure1 = m1; measure2 = m2) ->
        walkMeasure m1
        walkMeasure m2
      | SynMeasure.Divide(m1, _, m2, _) ->
        Option.iter walkMeasure m1
        walkMeasure m2
      | SynMeasure.Named _ -> ()
      | SynMeasure.Seq(ms, _) -> List.iter walkMeasure ms
      | SynMeasure.Power(m, _, _, _) -> walkMeasure m
      | SynMeasure.Var(ty, _) -> walkTypar ty
      | SynMeasure.Paren(m, _) -> walkMeasure m
      | SynMeasure.One _
      | SynMeasure.Anon _ -> ()

    and walkSimplePat s =
      walker.WalkSimplePat s

      match s with
      | SynSimplePat.Attrib(pat, AllAttrs attrs, _) ->
        walkSimplePat pat
        List.iter walkAttribute attrs
      | SynSimplePat.Typed(pat, t, _) ->
        walkSimplePat pat
        walkType t
      | SynSimplePat.Id _ -> ()

    and walkField (SynField(attributes = AllAttrs attrs; fieldType = t; range = _) as s) =
      walker.WalkField s
      List.iter walkAttribute attrs
      walkType t

    and walkValSig
      (SynValSig(attributes = AllAttrs attrs; synType = t; arity = SynValInfo(argInfos, argInfo); range = _) as s)
      =
      walker.WalkValSig s
      List.iter walkAttribute attrs
      walkType t

      argInfo :: (argInfos |> List.concat)
      |> List.collect (fun (SynArgInfo(attributes = AllAttrs attrs)) -> attrs)
      |> List.iter walkAttribute

    and walkMemberSig s =
      walker.WalkMemberSig s

      match s with
      | SynMemberSig.Inherit(t, _)
      | SynMemberSig.Interface(t, _) -> walkType t
      | SynMemberSig.Member(vs, _, _, _) -> walkValSig vs
      | SynMemberSig.ValField(f, _) -> walkField f
      | SynMemberSig.NestedType(SynTypeDefnSig(typeInfo = info; typeRepr = repr; members = memberSigs), _) ->

        walkComponentInfo info
        walkTypeDefnSigRepr repr
        List.iter walkMemberSig memberSigs

    and walkMember s =
      walker.WalkMember s

      match s with
      | SynMemberDefn.AbstractSlot(valSig, _, _, _) -> walkValSig valSig
      | SynMemberDefn.Member(binding, _) -> walkBinding binding
      | SynMemberDefn.ImplicitCtor(attributes = AllAttrs attrs; ctorArgs = ctorPattern) ->
        List.iter walkAttribute attrs
        walkPat ctorPattern
      | SynMemberDefn.ImplicitInherit(inheritType = t; inheritArgs = e) ->
        walkType t
        walkExpr e
      | SynMemberDefn.LetBindings(bindings, _, _, _) -> List.iter walkBinding bindings
      | SynMemberDefn.Interface(t, _, members, _) ->
        walkType t
        members |> Option.iter (List.iter walkMember)
      | SynMemberDefn.Inherit(baseType = t) -> t |> Option.iter walkType
      | SynMemberDefn.ValField(field, _) -> walkField field
      | SynMemberDefn.NestedType(tdef, _, _) -> walkTypeDefn tdef
      | SynMemberDefn.AutoProperty(attributes = AllAttrs attrs; typeOpt = t; synExpr = e; range = _) ->
        List.iter walkAttribute attrs
        Option.iter walkType t
        walkExpr e
      | SynMemberDefn.Open _ -> ()
      | SynMemberDefn.GetSetMember(memberDefnForGet = getter; memberDefnForSet = setter; range = _) ->
        Option.iter walkBinding getter
        Option.iter walkBinding setter

    and walkEnumCase (SynEnumCase(attributes = AllAttrs attrs; range = _) as s) =
      walker.WalkEnumCase s
      List.iter walkAttribute attrs

    and walkUnionCaseType s =
      walker.WalkUnionCaseType s

      match s with
      | SynUnionCaseKind.Fields fields -> List.iter walkField fields
      | SynUnionCaseKind.FullType(t, _) -> walkType t

    and walkUnionCase (SynUnionCase(attributes = AllAttrs attrs; caseType = t; range = _) as s) =
      walker.WalkUnionCase s
      List.iter walkAttribute attrs
      walkUnionCaseType t

    and walkTypeDefnSimple s =
      walker.WalkTypeDefnSimple s

      match s with
      | SynTypeDefnSimpleRepr.Enum(cases, _) -> List.iter walkEnumCase cases
      | SynTypeDefnSimpleRepr.Union(_, cases, _) -> List.iter walkUnionCase cases
      | SynTypeDefnSimpleRepr.Record(_, fields, _) -> List.iter walkField fields
      | SynTypeDefnSimpleRepr.TypeAbbrev(_, t, _) -> walkType t
      | SynTypeDefnSimpleRepr.General _ -> ()
      | SynTypeDefnSimpleRepr.LibraryOnlyILAssembly _ -> ()
      | SynTypeDefnSimpleRepr.None _ -> ()
      | SynTypeDefnSimpleRepr.Exception _ -> ()

    and walkComponentInfo
      (SynComponentInfo(
        attributes = AllAttrs attrs; typeParams = typars; constraints = constraints; longId = _; range = _) as s)
      =
      walker.WalkComponentInfo s
      List.iter walkAttribute attrs
      Option.iter walkTyparDecls typars
      List.iter walkTypeConstraint constraints

    and walkTypeDefnRepr s =
      walker.WalkTypeDefnRepr s

      match s with
      | SynTypeDefnRepr.ObjectModel(_, defns, _) -> List.iter walkMember defns
      | SynTypeDefnRepr.Simple(defn, _) -> walkTypeDefnSimple defn
      | SynTypeDefnRepr.Exception _ -> ()

    and walkTypeDefnSigRepr s =
      walker.WalkTypeDefnSigRepr s

      match s with
      | SynTypeDefnSigRepr.ObjectModel(_, defns, _) -> List.iter walkMemberSig defns
      | SynTypeDefnSigRepr.Simple(defn, _) -> walkTypeDefnSimple defn
      | SynTypeDefnSigRepr.Exception _ -> ()

    and walkTypeDefn (SynTypeDefn(info, repr, members, implicitCtor, _, _) as s) =
      walker.WalkTypeDefn s

      walkComponentInfo info
      walkTypeDefnRepr repr
      Option.iter walkMember implicitCtor
      List.iter walkMember members

    and walkSynModuleDecl (decl: SynModuleDecl) =
      walker.WalkSynModuleDecl decl

      match decl with
      | SynModuleDecl.NamespaceFragment fragment -> walkSynModuleOrNamespace fragment
      | SynModuleDecl.NestedModule(info, _, modules, _, _, _) ->
        walkComponentInfo info
        List.iter walkSynModuleDecl modules
      | SynModuleDecl.Let(_, bindings, _) -> List.iter walkBinding bindings
      | SynModuleDecl.Expr(expr, _) -> walkExpr expr
      | SynModuleDecl.Types(types, _) -> List.iter walkTypeDefn types
      | SynModuleDecl.Attributes(attributes = AllAttrs attrs; range = _) -> List.iter walkAttribute attrs
      | SynModuleDecl.ModuleAbbrev _ -> ()
      | SynModuleDecl.Exception _ -> ()
      | SynModuleDecl.Open _ -> ()
      | SynModuleDecl.HashDirective _ -> ()


    match input with
    | ParsedInput.ImplFile input -> walkImplFileInput input
    | _ -> ()

namespace FsAutoComplete

module UntypedAstUtils =

  open FSharp.Compiler.Syntax
  open FSharp.Compiler.Text

  type Range with

    member inline x.IsEmpty = x.StartColumn = x.EndColumn && x.StartLine = x.EndLine

  type internal ShortIdent = string
  type internal Idents = ShortIdent[]

  let internal longIdentToArray (longIdent: LongIdent) : Idents = longIdent |> Seq.map string |> Seq.toArray

  /// matches if the range contains the position
  let (|ContainsPos|_|) pos range = if Range.rangeContainsPos range pos then Some() else None

  /// Active pattern that matches an ident on a given name by the ident's `idText`
  let (|Ident|_|) ofName =
    function
    | SynExpr.Ident ident when ident.idText = ofName -> Some()
    | _ -> None

  /// matches if the range contains the position
  let (|IdentContainsPos|_|) pos (ident: Ident) = (|ContainsPos|_|) pos ident.idRange

module FoldingRange =
  open FSharp.Compiler.Text
  open FSharp.Compiler.Syntax

  /// a walker that collects all ranges of syntax elements that contain the given position
  [<RequireQualifiedAccess>]
  type private RangeCollectorWalker(pos: Position) =
    inherit SyntaxCollectorBase()
    let ranges = ResizeArray<Range>()

    let addIfInside (m: Range) =
      if (Range.rangeContainsPos m pos) then
        ranges.Add m

    override _.WalkSynModuleOrNamespace m = addIfInside m.Range
    override _.WalkAttribute a = addIfInside a.Range
    override _.WalkTypeConstraint c = addIfInside c.Range
    override _.WalkPat p = addIfInside p.Range

    override _.WalkBinding(SynBinding(range = r; returnInfo = ri)) =
      addIfInside r
      ri |> Option.iter (fun (SynBindingReturnInfo(range = r')) -> addIfInside r')

    override _.WalkInterfaceImpl(SynInterfaceImpl(range = range)) = addIfInside range
    override _.WalkType t = addIfInside t.Range
    override _.WalkClause c = addIfInside c.Range

    override _.WalkInterpolatedStringPart i =
      match i with
      | SynInterpolatedStringPart.FillExpr(qualifiers = Some ident) -> addIfInside ident.idRange
      | SynInterpolatedStringPart.String(_, r) -> addIfInside r
      | _ -> ()

    override _.WalkExpr e = addIfInside e.Range

    override _.WalkMeasure m =
      match m with
      | SynMeasure.Product(range = r)
      | SynMeasure.Divide(range = r)
      | SynMeasure.Named(range = r)
      | SynMeasure.Seq(range = r)
      | SynMeasure.Power(range = r)
      | SynMeasure.Var(range = r)
      | SynMeasure.Paren(range = r)
      | SynMeasure.One(range = r)
      | SynMeasure.Anon(range = r) -> addIfInside r

    override _.WalkSimplePat p = addIfInside p.Range
    override _.WalkField(SynField(range = r)) = addIfInside r
    override _.WalkValSig(SynValSig(range = r)) = addIfInside r
    override _.WalkMemberSig m = addIfInside m.Range
    override _.WalkMember m = addIfInside m.Range
    override _.WalkEnumCase e = addIfInside e.Range
    override _.WalkUnionCase u = addIfInside u.Range
    override _.WalkTypeDefnSimple s = addIfInside s.Range
    override _.WalkComponentInfo c = addIfInside c.Range
    override _.WalkTypeDefnRepr t = addIfInside t.Range
    override _.WalkTypeDefnSigRepr t = addIfInside t.Range
    override _.WalkTypeDefn t = addIfInside t.Range
    override _.WalkSynModuleDecl s = addIfInside s.Range

    member _.Ranges = ranges

  let getRangesAtPosition input (r: Position) : Range list =
    let walker = RangeCollectorWalker(r)
    walkAst walker input
    walker.Ranges |> Seq.toList

module Completion =
  open FSharp.Compiler.Text
  open FSharp.Compiler.Syntax

  [<RequireQualifiedAccess>]
  type Context =
    | StringLiteral
    | Unknown
    | SynType

  let atPos (pos: Position, ast: ParsedInput) : Context =
    (pos, ast)
    ||> ParsedInput.tryPick (fun _path node ->
      match node with
      | SyntaxNode.SynType _ -> Some Context.SynType
      | _ -> None)
    |> Option.orElseWith (fun () ->
      ast
      |> ParsedInput.tryNode pos
      |> Option.bind (fun (node, _path) ->
        match node with
        | SyntaxNode.SynExpr(SynExpr.Const(SynConst.String _, _)) -> Some Context.StringLiteral
        | _ -> None))
    |> Option.defaultValue Context.Unknown
