namespace FsAutoComplete

module DocumentationFormatter =

  open FSharp.Compiler.CodeAnalysis
  open FSharp.Compiler.EditorServices
  open FSharp.Compiler.Symbols
  open FSharp.Compiler.Syntax
  open FSharp.Compiler.Tokenization
  open System
  open System.Text
  open System.Text.RegularExpressions

  let nl = Environment.NewLine
  let maxPadding = 200

  let mutable lastDisplayContext: FSharpDisplayContext = FSharpDisplayContext.Empty

  type EntityInfo =
    { Constructors: string array
      Fields: string array
      Functions: string array
      Interfaces: string array
      Attributes: string array
      DeclaredTypes: string array }

    static member Empty =

      { Constructors = [||]
        Fields = [||]
        Functions = [||]
        Interfaces = [||]
        Attributes = [||]
        DeclaredTypes = [||] }

  /// Concat two strings with a space between if both a and b are not IsNullOrWhiteSpace
  let internal (++) (a: string) (b: string) =
    match String.IsNullOrEmpty a, String.IsNullOrEmpty b with
    | true, true -> ""
    | false, true -> a
    | true, false -> b
    | false, false -> a + " " + b

  let internal formatShowDocumentationLink (name: string) xmlDocSig assemblyName =
    if assemblyName = "-" || xmlDocSig = "_" then
      name, name.Length
    else
      let content =
        Uri.EscapeDataString(sprintf """[{ "XmlDocSig": "%s", "AssemblyName": "%s" }]""" xmlDocSig assemblyName)

      $"<a href='command:fsharp.showDocumentation?%s{content}'>%s{name}</a>", name.Length

  let tag = Regex """<.*>"""

  let rec formatType (displayContext: FSharpDisplayContext) (typ: FSharpType) : string * int =
    let combineParts (parts: (string * int) seq) : string * int =
      // make a single type name out of all of the tuple parts, since each part is correct by construction
      (("", 0), parts) ||> Seq.fold (fun (s, l) (ps, pl) -> (s + ps), (l + pl))

    let resolvedType =
      // unwrap any type abbreviations to their wrapped type
      if typ.IsAbbreviation then typ.AbbreviatedType else typ

    let xmlDocSig =
      try
        // if this type resolves to an actual type, then get the xmldoc signature of it, if any.
        // some times
        if resolvedType.HasTypeDefinition then
          resolvedType.TypeDefinition.XmlDocSig
        else
          "-"
      with _ ->
        "-"

    let assemblyName =
      try
        if resolvedType.IsGenericParameter then
          // generic parameters are unsolved, they don't correspond to actual types,
          // so we can't get the assembly name from them
          "-"
        else
          resolvedType.TypeDefinition.Assembly.SimpleName
      with _ ->
        "-"

    if typ.IsTupleType || typ.IsStructTupleType then
      // tuples are made of individual type names for the elements separated by asterisks
      let separator = formatShowDocumentationLink " * " xmlDocSig assemblyName

      let parts =
        typ.GenericArguments
        |> Seq.map (formatType displayContext)
        |> Seq.intersperse separator
        |> List.ofSeq

      combineParts parts

    elif typ.HasTypeDefinition && typ.GenericArguments.Count > 0 then
      // this type has generic arguments, so we need to format each of them.
      // types with generic arguments get rendered as TYPENAME<P1, P2, P3, ..., PN>
      let renderedGenericArgumentTypes =
        typ.GenericArguments |> Seq.map (formatType displayContext)
      // we set this context specifically because we want to enforce prefix-generic form on tooltip displays
      let newContext = displayContext.WithPrefixGenericParameters()
      let org = typ.Format newContext
      let t = tag.Replace(org, "<")

      [ yield formatShowDocumentationLink t xmlDocSig assemblyName
        if t.EndsWith("<", StringComparison.Ordinal) then
          yield! renderedGenericArgumentTypes |> Seq.intersperse (", ", 2)

          yield formatShowDocumentationLink ">" xmlDocSig assemblyName ]
      |> combineParts

    elif typ.IsGenericParameter then
      let prefix = if typ.GenericParameter.IsSolveAtCompileTime then "^" else "'"
      let name = prefix + typ.GenericParameter.Name

      formatShowDocumentationLink name xmlDocSig assemblyName
    else if typ.HasTypeDefinition then
      let name =
        typ.TypeDefinition.DisplayName |> FSharpKeywords.NormalizeIdentifierBackticks

      formatShowDocumentationLink name xmlDocSig assemblyName
    else
      let name = typ.Format displayContext
      formatShowDocumentationLink name xmlDocSig assemblyName


  let format displayContext (typ: FSharpType) : (string * int) = formatType displayContext typ

  let formatGenericParameter includeMemberConstraintTypes displayContext (param: FSharpGenericParameter) =

    let asGenericParamName (param: FSharpGenericParameter) = (if param.IsSolveAtCompileTime then "^" else "'") + param.Name

    let sb = StringBuilder()

    let getConstraint (constrainedBy: FSharpGenericParameterConstraint) =
      let memberConstraint (c: FSharpGenericParameterMemberConstraint) =
        let formattedMemberName, isProperty =
          match c.IsProperty, PrettyNaming.TryChopPropertyName c.MemberName with
          | true, Some(chopped) when chopped <> c.MemberName -> chopped, true
          | _, _ ->
            if PrettyNaming.IsLogicalOpName c.MemberName then
              PrettyNaming.ConvertValLogicalNameToDisplayNameCore c.MemberName, false
            else
              c.MemberName, false

        seq {
          if c.MemberIsStatic then
            yield "static "

          yield "member "
          yield formattedMemberName

          if includeMemberConstraintTypes then
            yield " : "

            if isProperty then
              yield (c.MemberReturnType |> format displayContext |> fst)
            else
              if c.MemberArgumentTypes.Count <= 1 then
                yield "unit"
              else
                yield asGenericParamName param

              yield " -> "

              yield ((c.MemberReturnType |> format displayContext |> fst).TrimStart())
        }
        |> String.concat ""

      let typeConstraint (tc: FSharpType) = sprintf ":> %s" (tc |> format displayContext |> fst)

      let enumConstraint (ec: FSharpType) = sprintf "enum<%s>" (ec |> format displayContext |> fst)

      let delegateConstraint (tc: FSharpGenericParameterDelegateConstraint) =
        sprintf
          "delegate<%s, %s>"
          (tc.DelegateTupledArgumentType |> format displayContext |> fst)
          (tc.DelegateReturnType |> format displayContext |> fst)

      let symbols =
        match constrainedBy with
        | _ when constrainedBy.IsCoercesToConstraint -> Some(typeConstraint constrainedBy.CoercesToTarget)
        | _ when constrainedBy.IsMemberConstraint -> Some(memberConstraint constrainedBy.MemberConstraintData)
        | _ when constrainedBy.IsSupportsNullConstraint -> Some("null")
        | _ when constrainedBy.IsRequiresDefaultConstructorConstraint -> Some("default constructor")
        | _ when constrainedBy.IsReferenceTypeConstraint -> Some("reference")
        | _ when constrainedBy.IsEnumConstraint -> Some(enumConstraint constrainedBy.EnumConstraintTarget)
        | _ when constrainedBy.IsComparisonConstraint -> Some("comparison")
        | _ when constrainedBy.IsEqualityConstraint -> Some("equality")
        | _ when constrainedBy.IsDelegateConstraint -> Some(delegateConstraint constrainedBy.DelegateConstraintData)
        | _ when constrainedBy.IsUnmanagedConstraint -> Some("unmanaged")
        | _ when constrainedBy.IsNonNullableValueTypeConstraint -> Some("struct")
        | _ -> None

      symbols

    if param.Constraints.Count > 0 then
      param.Constraints
      |> Seq.choose getConstraint
      |> Seq.distinct
      |> Seq.iteri (fun i symbol ->
        if i > 0 then
          print sb " and "

        print sb symbol)

    sb.ToString()

  let formatParameter displayContext (p: FSharpParameter) =
    try
      p.Type |> format displayContext
    with :? InvalidOperationException ->
      p.DisplayName, p.DisplayName.Length

  let getUnionCaseSignature displayContext (unionCase: FSharpUnionCase) =
    if unionCase.Fields.Count > 0 then
      let typeList =
        unionCase.Fields
        |> Seq.map (fun unionField ->
          if unionField.Name.StartsWith("Item", StringComparison.Ordinal) then //TODO: Some better way of detecting default names for the union cases' fields
            unionField.FieldType |> format displayContext |> fst

          else
            unionField.Name
            ++ ":"
            ++ ((unionField.FieldType |> format displayContext |> fst)))
        |> String.concat " * "

      unionCase.DisplayName + " of " + typeList
    else
      unionCase.DisplayName

  let getFuncSignatureWithIdent displayContext (func: FSharpMemberOrFunctionOrValue) (ident: int) =
    let maybeGetter = func.LogicalName.StartsWith("get_", StringComparison.Ordinal)
    let indent = String.replicate ident " "

    let functionName =
      let name =
        if func.IsConstructor then
          match func.EnclosingEntitySafe with
          | Some ent -> ent.DisplayName
          | _ -> func.DisplayName
          |> FSharpKeywords.NormalizeIdentifierBackticks
        elif func.IsOperatorOrActivePattern then
          func.DisplayName
        elif func.DisplayName.StartsWith("( ", StringComparison.Ordinal) then
          FSharpKeywords.NormalizeIdentifierBackticks func.LogicalName
        else
          FSharpKeywords.NormalizeIdentifierBackticks func.DisplayName

      name

    let modifiers =
      let accessibility =
        match func.Accessibility with
        | a when a.IsInternal -> "internal"
        | a when a.IsPrivate -> "private"
        | _ -> ""

      let modifier =
        //F# types are prefixed with new, should non F# types be too for consistency?
        if func.IsConstructor then
          match func.EnclosingEntitySafe with
          | Some ent ->
            if ent.IsFSharp then
              "new" ++ accessibility
            else
              accessibility
          | _ -> accessibility
        elif func.IsProperty then
          if func.IsInstanceMember then
            if func.IsDispatchSlot then
              "abstract property" ++ accessibility
            else
              "property" ++ accessibility
          else
            "static property" ++ accessibility
        elif func.IsMember then
          if func.IsInstanceMember then
            if func.IsDispatchSlot then
              "abstract member" ++ accessibility
            else
              "member" ++ accessibility
          else
            "static member" ++ accessibility
        else if func.InlineAnnotation = FSharpInlineAnnotation.AlwaysInline then
          "val" ++ accessibility ++ "inline"
        elif func.IsInstanceMember then
          "val" ++ accessibility
        else
          "val" ++ accessibility //does this need to be static prefixed?

      modifier

    let argInfos = func.CurriedParameterGroups |> Seq.map Seq.toList |> Seq.toList

    let retType =
      //This try block will be removed when FCS updates
      try
        func.ReturnParameter.Type |> format displayContext |> fst
      with _ex ->
        "Unknown"

    let retTypeConstraint =
      if func.ReturnParameter.Type.IsGenericParameter then
        let formattedParam =
          formatGenericParameter false displayContext func.ReturnParameter.Type.GenericParameter

        if String.IsNullOrWhiteSpace formattedParam then
          formattedParam
        else
          "(requires " + formattedParam + " )"
      else
        ""

    let padLength =
      let allLengths =
        argInfos
        |> List.concat
        |> List.map (fun p ->
          let name = Option.defaultValue p.DisplayName p.Name
          let normalisedName = FSharpKeywords.NormalizeIdentifierBackticks name
          normalisedName.Length)

      match allLengths with
      | [] -> 0
      | l -> l |> List.maxUnderThreshold maxPadding

    let formatName indent padding (parameter: FSharpParameter) =
      let name = Option.defaultValue parameter.DisplayName parameter.Name
      let normalisedName = FSharpKeywords.NormalizeIdentifierBackticks name
      indent + normalisedName.PadRight padding + ":"

    let isDelegate =
      match func.EnclosingEntitySafe with
      | Some ent -> ent.IsDelegate
      | _ -> false

    match argInfos with
    | [] ->
      //When does this occur, val type within  module?
      if isDelegate then
        retType
      else
        modifiers ++ functionName + ":" ++ retType

    | [ [] ] ->
      if isDelegate then
        retType
      //A ctor with () parameters seems to be a list with an empty list.
      // Also abstract members and abstract member overrides with one () parameter seem to be a list with an empty list.
      elif func.IsConstructor || (func.IsMember && (not func.IsPropertyGetterMethod)) then
        modifiers + ": unit -> " ++ retType
      else
        modifiers ++ functionName + ":" ++ retType //Value members seems to be a list with an empty list
    | [ [ p ] ] when maybeGetter && formatParameter displayContext p |> fst = "unit" -> //Member or property with only getter
      modifiers ++ functionName + ":" ++ retType
    | many ->

      let allParamsLengths =
        many
        |> List.map (List.map (fun p -> (formatParameter displayContext p |> snd)) >> List.sum)

      let maxLength = (allParamsLengths |> List.maxUnderThreshold maxPadding) + 1

      let parameterTypeWithPadding (p: FSharpParameter) length =
        (formatParameter displayContext p |> fst)
        + (String.replicate (if length >= maxLength then 1 else maxLength - length) " ")

      let formatParameterPadded length p =
        let paddedParam =
          formatName indent padLength p ++ (parameterTypeWithPadding p length)

        if p.Type.IsGenericParameter then
          let paramConstraint =
            let formattedParam =
              formatGenericParameter false displayContext p.Type.GenericParameter

            if String.IsNullOrWhiteSpace formattedParam then
              formattedParam
            else
              "(requires " + formattedParam + " )"

          if paramConstraint = retTypeConstraint then
            paddedParam
          else
            paddedParam + paramConstraint
        else
          paddedParam

      let allParams =
        List.zip many allParamsLengths
        |> List.map (fun (paramTypes, length) ->
          paramTypes |> List.map (formatParameterPadded length) |> String.concat $" *{nl}")
        |> String.concat $"->{nl}"

      let typeArguments =
        allParams + nl + indent + (String.replicate (max (padLength - 1) 0) " ") + "->"
        ++ retType
        ++ retTypeConstraint

      if isDelegate then
        typeArguments
      else
        modifiers ++ $"{functionName}:{nl}{typeArguments}"

  let getFuncSignatureForTypeSignature
    displayContext
    (func: FSharpMemberOrFunctionOrValue)
    (getter: bool)
    (setter: bool)
    =
    let functionName =
      let name =
        if func.IsConstructor then
          "new"
        elif func.IsOperatorOrActivePattern then
          func.DisplayName
        elif func.DisplayName.StartsWith("( ", StringComparison.Ordinal) then
          FSharpKeywords.NormalizeIdentifierBackticks func.LogicalName
        elif
          func.LogicalName.StartsWith("get_", StringComparison.Ordinal)
          || func.LogicalName.StartsWith("set_", StringComparison.Ordinal)
        then
          PrettyNaming.TryChopPropertyName func.DisplayName
          |> Option.defaultValue func.DisplayName
        else
          func.DisplayName

      fst (formatShowDocumentationLink name func.XmlDocSig func.Assembly.SimpleName)

    let modifiers =
      let accessibility =
        match func.Accessibility with
        | a when a.IsInternal -> "internal"
        | a when a.IsPrivate -> "private"
        | _ -> ""

      let modifier =
        //F# types are prefixed with new, should non F# types be too for consistency?
        if func.IsConstructor then
          accessibility
        elif func.IsProperty then
          if func.IsInstanceMember then
            if func.IsDispatchSlot then
              "abstract property" ++ accessibility
            else
              "property" ++ accessibility
          else
            "static property" ++ accessibility
        elif func.IsMember then
          if func.IsInstanceMember then
            if func.IsDispatchSlot then
              "abstract member" ++ accessibility
            else
              "member" ++ accessibility
          else
            "static member" ++ accessibility
        else if func.InlineAnnotation = FSharpInlineAnnotation.AlwaysInline then
          "val" ++ accessibility ++ "inline"
        elif func.IsInstanceMember then
          "val" ++ accessibility
        else
          "val" ++ accessibility //does this need to be static prefixed?

      modifier

    let argInfos = func.CurriedParameterGroups |> Seq.map Seq.toList |> Seq.toList

    let retType =
      //This try block will be removed when FCS updates
      try
        if func.IsConstructor then
          match func.EnclosingEntitySafe with
          | Some ent -> ent.DisplayName
          | _ -> func.DisplayName
        else
          func.ReturnParameter.Type |> format displayContext |> fst
      with _ex ->
        try
          if func.FullType.GenericArguments.Count > 0 then
            let lastArg = func.FullType.GenericArguments |> Seq.last
            lastArg |> format displayContext |> fst
          else
            "Unknown"
        with _ ->
          "Unknown"

    let formatName (parameter: FSharpParameter) = parameter.Name |> Option.defaultValue parameter.DisplayName

    let isDelegate =
      match func.EnclosingEntitySafe with
      | Some ent -> ent.IsDelegate
      | _ -> false

    let res =
      match argInfos with
      | [] ->
        //When does this occur, val type within  module?
        if isDelegate then
          retType
        else
          modifiers ++ functionName + ": " ++ retType

      | [ [] ] ->
        if isDelegate then
          retType
        elif func.IsConstructor then
          modifiers + ": unit ->" ++ retType //A ctor with () parameters seems to be a list with an empty list
        else
          modifiers ++ functionName + ": " ++ retType //Value members seems to be a list with an empty list
      | many ->
        let allParams =
          many
          |> List.map (fun (paramTypes) ->
            paramTypes
            |> List.map (fun p -> formatName p + ":" ++ fst (formatParameter displayContext p))
            |> String.concat (" * "))
          |> String.concat ("-> ")

        let typeArguments = allParams ++ "->" ++ retType

        if isDelegate then
          typeArguments
        else
          modifiers ++ functionName + ": " + typeArguments

    match getter, setter with
    | true, true -> res ++ "with get,set"
    | true, false -> res ++ "with get"
    | false, true -> res ++ "with set"
    | false, false -> res

  let getFuncSignature f c = getFuncSignatureWithIdent f c 3

  let getValSignature displayContext (v: FSharpMemberOrFunctionOrValue) =
    let retType = v.FullType |> format displayContext |> fst

    let prefix = if v.IsMutable then "val mutable" else "val"

    let name =
      (if v.DisplayName.StartsWith("( ", StringComparison.Ordinal) then
         v.LogicalName
       else
         v.DisplayName)
      |> FSharpKeywords.NormalizeIdentifierBackticks

    let constraints =
      match v.FullTypeSafe with
      | Some fullType when fullType.IsGenericParameter ->
        let formattedParam =
          formatGenericParameter false displayContext fullType.GenericParameter

        if String.IsNullOrWhiteSpace formattedParam then
          None
        else
          Some formattedParam
      | _ -> None

    match constraints with
    | Some constraints -> prefix ++ name ++ ":" ++ constraints
    | None -> prefix ++ name ++ ":" ++ retType

  let getFieldSignature displayContext (field: FSharpField) =
    let retType = field.FieldType |> format displayContext |> fst

    match field.LiteralValue with
    | Some lv -> field.DisplayName + ":" ++ retType ++ "=" ++ (string lv)
    | None ->
      let prefix = if field.IsMutable then "val mutable" else "val"

      prefix ++ field.DisplayName + ":" ++ retType

  let getAPCaseSignature displayContext (apc: FSharpActivePatternCase) =
    let findVal =
      apc.Group.DeclaringEntity
      |> Option.bind (fun ent ->
        ent.MembersFunctionsAndValues
        |> Seq.tryFind (fun func -> func.DisplayName.Contains apc.DisplayName)
        |> Option.map (getFuncSignature displayContext))
      |> Option.bind (fun n ->
        try
          Some(n.Split([| ':' |], 2).[1])
        with _ ->
          None)
      |> Option.defaultValue ""

    sprintf "active pattern %s: %s" apc.Name findVal

  let getAttributeSignature (attr: FSharpAttribute) =
    let name =
      formatShowDocumentationLink
        attr.AttributeType.DisplayName
        attr.AttributeType.XmlDocSig
        attr.AttributeType.Assembly.SimpleName

    let attr =
      attr.ConstructorArguments |> Seq.map (snd >> string) |> String.concat ", "

    sprintf "%s(%s)" (fst name) attr


  let getEntitySignature displayContext (fse: FSharpEntity) =
    let modifier =
      match fse.Accessibility with
      | a when a.IsInternal -> "internal "
      | a when a.IsPrivate -> "private "
      | _ -> ""

    let typeName (fse: FSharpEntity) =
      match fse with
      | _ when fse.IsFSharpModule -> "module"
      | _ when fse.IsEnum -> "enum"
      | _ when fse.IsValueType -> "struct"
      | _ when fse.IsNamespace -> "namespace"
      | _ when fse.IsFSharpRecord -> "record"
      | _ when fse.IsFSharpUnion -> "union"
      | _ when fse.IsInterface -> "interface"
      | _ -> "type"

    let enumTip () =
      $" ={nl}  |"
      ++ (fse.FSharpFields
          |> Seq.filter (fun f -> not f.IsCompilerGenerated)
          |> Seq.sortBy (fun f ->
            match f.LiteralValue with
            | None -> -1
            | Some lv -> Int32.Parse(string lv))
          |> Seq.map (fun field ->
            match field.LiteralValue with
            | Some lv -> field.Name + " = " + (string lv)
            | None -> field.Name)
          |> String.concat $"{nl}  | ")

    let unionTip () =
      $" ={nl}  |"
      ++ (fse.UnionCases
          |> Seq.map (getUnionCaseSignature displayContext)
          |> String.concat ($"{nl}  | "))

    let delegateTip () =
      let invoker =
        fse.MembersFunctionsAndValues |> Seq.find (fun f -> f.DisplayName = "Invoke")

      let invokerSig = getFuncSignatureWithIdent displayContext invoker 6
      $" ={nl}   delegate of{nl}{invokerSig}"

    let typeTip () =
      let constructors =
        fse.MembersFunctionsAndValues
        |> Seq.filter (fun n -> n.IsConstructor && n.Accessibility.IsPublic)
        |> Seq.collect (fun f ->
          seq {
            yield getFuncSignatureForTypeSignature displayContext f false false

            yield!
              f.GetOverloads false
              |> Option.map (Seq.map (fun f -> getFuncSignatureForTypeSignature displayContext f false false))
              |> Option.defaultValue Seq.empty
          })

        |> Seq.toArray

      let fields =
        fse.FSharpFields
        |> Seq.filter (fun n -> n.Accessibility.IsPublic) //TODO: If defined in same project as current scope then show also internals
        |> Seq.sortBy (fun n -> n.DisplayName)
        |> Seq.map (getFieldSignature displayContext)
        |> Seq.toArray

      let funcs =
        fse.MembersFunctionsAndValues
        |> Seq.filter (fun n -> n.Accessibility.IsPublic && (not n.IsConstructor)) //TODO: If defined in same project as current scope then show also internals
        |> Seq.sortWith (fun n1 n2 ->
          let modifierScore (f: FSharpMemberOrFunctionOrValue) =
            if f.IsProperty then
              if f.IsInstanceMember then
                if f.IsDispatchSlot then 9 else 1
              else
                8
            elif f.IsMember then
              if f.IsInstanceMember then
                if f.IsDispatchSlot then 11 else 2
              else
                10
            else
              3

          let n1Score = modifierScore n1
          let n2Score = modifierScore n2

          if n1Score = n2Score then
            n1.DisplayName.CompareTo n2.DisplayName
          else
            n1Score.CompareTo n2Score)
        |> Seq.groupBy (fun n -> n.FullName)
        |> Seq.collect (fun (_, v) ->
          match v |> Seq.tryFind (fun f -> f.IsProperty) with
          | Some prop ->
            let getter = v |> Seq.exists (fun f -> f.IsPropertyGetterMethod)

            let setter = v |> Seq.exists (fun f -> f.IsPropertySetterMethod)

            [ getFuncSignatureForTypeSignature displayContext prop getter setter ] //Ensure properties are displayed only once, properly report
          | None ->
            [ for f in v do
                yield getFuncSignatureForTypeSignature displayContext f false false

                yield!
                  f.GetOverloads false
                  |> Option.map (Seq.map (fun f -> getFuncSignatureForTypeSignature displayContext f false false))
                  |> Option.defaultValue Seq.empty ])
        |> Seq.toArray

      let interfaces =
        fse.DeclaredInterfaces
        |> Seq.map (fun inf -> fst (format displayContext inf))
        |> Seq.toArray

      let attrs = fse.Attributes |> Seq.map getAttributeSignature |> Seq.toArray

      let types =
        fse.NestedEntities
        |> Seq.choose (fun ne ->
          let isCompilerGenerated =
            ne.Attributes
            |> Seq.tryFind (fun attribute -> attribute.AttributeType.CompiledName = "CompilerGeneratedAttribute")
            |> Option.isSome

          if not ne.IsNamespace && not isCompilerGenerated then
            (typeName ne)
            ++ fst (formatShowDocumentationLink ne.DisplayName ne.XmlDocSig ne.Assembly.SimpleName)
            |> Some
          else
            None)
        |> Seq.toArray

      { Constructors = constructors
        Fields = fields
        Functions = funcs
        Interfaces = interfaces
        Attributes = attrs
        DeclaredTypes = types }

    let typeDisplay =
      let name =
        let normalisedName = FSharpKeywords.NormalizeIdentifierBackticks fse.DisplayName

        if fse.GenericParameters.Count > 0 then
          let paramsAndConstraints =
            fse.GenericParameters
            |> Seq.groupBy (fun p -> p.Name)
            |> Seq.map (fun (name, constraints) ->
              let renderedConstraints =
                constraints
                |> Seq.map (formatGenericParameter false displayContext)
                |> String.concat " and"

              if String.IsNullOrWhiteSpace renderedConstraints then
                "'" + name
              else
                sprintf "'%s (requires %s)" name renderedConstraints)

          normalisedName + "<" + (paramsAndConstraints |> String.concat ",") + ">"
        else
          normalisedName

      let basicName = modifier + (typeName fse) ++ name

      if fse.IsFSharpAbbreviation then
        let unannotatedType = fse.UnAnnotate()
        basicName ++ "=" ++ (unannotatedType.DisplayName)
      else
        basicName

    if fse.IsFSharpUnion then
      (typeDisplay + unionTip ()), typeTip ()
    elif fse.IsEnum then
      (typeDisplay + enumTip ()), EntityInfo.Empty
    elif fse.IsDelegate then
      (typeDisplay + delegateTip ()), EntityInfo.Empty
    else
      typeDisplay, typeTip ()

  type FSharpSymbol with

    /// trims the leading 'Microsoft.' from the full name of the symbol
    member m.SafeFullName =
      if
        m.FullName.StartsWith("Microsoft.", StringComparison.Ordinal)
        && m.Assembly.SimpleName = "FSharp.Core"
      then
        m.FullName.Substring "Microsoft.".Length
      else
        m.FullName

  let footerForType (entity: FSharpSymbolUse) =
    try
      match entity with
      | SymbolUse.MemberFunctionOrValue m ->
        match m.DeclaringEntity with
        | None -> sprintf "Full name: %s\nAssembly: %s" m.SafeFullName m.Assembly.SimpleName
        | Some e ->
          let link =
            fst (formatShowDocumentationLink e.DisplayName e.XmlDocSig e.Assembly.SimpleName)

          sprintf "Full name: %s\nDeclaring Entity: %s\nAssembly: %s" m.SafeFullName link m.Assembly.SimpleName

      | SymbolUse.Entity(c, _) ->
        match c.DeclaringEntity with
        | None -> sprintf "Full name: %s\nAssembly: %s" c.SafeFullName c.Assembly.SimpleName
        | Some e ->
          let link =
            fst (formatShowDocumentationLink e.DisplayName e.XmlDocSig e.Assembly.SimpleName)

          sprintf "Full name: %s\nDeclaring Entity: %s\nAssembly: %s" c.SafeFullName link c.Assembly.SimpleName


      | SymbolUse.Field f ->
        match f.DeclaringEntity with
        | None -> sprintf "Full name: %s\nAssembly: %s" f.SafeFullName f.Assembly.SimpleName
        | Some e ->
          let link =
            fst (formatShowDocumentationLink e.DisplayName e.XmlDocSig e.Assembly.SimpleName)

          sprintf "Full name: %s\nDeclaring Entity: %s\nAssembly: %s" f.SafeFullName link f.Assembly.SimpleName

      | SymbolUse.ActivePatternCase ap -> sprintf "Full name: %s\nAssembly: %s" ap.SafeFullName ap.Assembly.SimpleName

      | SymbolUse.UnionCase uc -> sprintf "Full name: %s\nAssembly: %s" uc.SafeFullName uc.Assembly.SimpleName
      | _ -> ""
    with _ ->
      ""

  let footerForType' (entity: FSharpSymbol) =
    try
      match entity with
      | MemberFunctionOrValue m -> sprintf "Full name: %s\nAssembly: %s" m.SafeFullName m.Assembly.SimpleName

      | EntityFromSymbol(c, _) -> sprintf "Full name: %s\nAssembly: %s" c.SafeFullName c.Assembly.SimpleName

      | Field(f, _) -> sprintf "Full name: %s\nAssembly: %s" f.SafeFullName f.Assembly.SimpleName

      | ActivePatternCase ap -> sprintf "Full name: %s\nAssembly: %s" ap.SafeFullName ap.Assembly.SimpleName

      | UnionCase uc -> sprintf "Full name: %s\nAssembly: %s" uc.SafeFullName uc.Assembly.SimpleName
      | _ -> ""
    with _ ->
      ""

  let compiledNameType (entity: FSharpSymbolUse) =
    try
      entity.Symbol.XmlDocSig
    with _ ->
      ""

  let compiledNameType' (entity: FSharpSymbol) =
    try
      entity.XmlDocSig
    with _ ->
      ""

  /// Returns formatted symbol signature and footer that can be used to enhance standard FCS' text tooltips
  let getTooltipDetailsFromSymbolUse (symbol: FSharpSymbolUse) =
    let cn = compiledNameType symbol
    lastDisplayContext <- symbol.DisplayContext

    match symbol with
    | SymbolUse.TypeAbbreviation(fse) ->
      try
        let parent = fse.GetAbbreviatedParent()

        match parent with
        | FSharpEntity(ent, _, _) ->
          let signature = getEntitySignature symbol.DisplayContext ent
          Some(signature, footerForType' parent, cn)
        | _ -> None
      with _ ->
        None

    | SymbolUse.Entity(fse, _) ->
      try
        let signature = getEntitySignature symbol.DisplayContext fse
        Some(signature, footerForType symbol, cn)
      with _ ->
        None

    | SymbolUse.Constructor func ->
      match func.EnclosingEntitySafe with
      | Some ent when ent.IsValueType || ent.IsEnum ->
        //ValueTypes
        let signature = getFuncSignature symbol.DisplayContext func
        Some((signature, EntityInfo.Empty), footerForType symbol, cn)
      | _ ->
        //ReferenceType constructor
        let signature = getFuncSignature symbol.DisplayContext func
        Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | SymbolUse.Operator func ->
      let signature = getFuncSignature symbol.DisplayContext func
      Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | SymbolUse.Pattern func ->
      //Active pattern or operator
      let signature = getFuncSignature symbol.DisplayContext func
      Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | SymbolUse.Property prop ->
      let signature = getFuncSignature symbol.DisplayContext prop
      Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | SymbolUse.ClosureOrNestedFunction func ->
      //represents a closure or nested function
      let signature = getFuncSignature symbol.DisplayContext func
      Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | SymbolUse.Function func ->
      let signature = getFuncSignature symbol.DisplayContext func
      Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | SymbolUse.Val func ->
      //val name : Type
      let signature = getValSignature symbol.DisplayContext func
      Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | SymbolUse.Field fsf ->
      let signature = getFieldSignature symbol.DisplayContext fsf
      Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | SymbolUse.UnionCase uc ->
      let signature = getUnionCaseSignature symbol.DisplayContext uc
      Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | SymbolUse.ActivePatternCase apc ->
      let signature = getAPCaseSignature symbol.DisplayContext apc
      Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | SymbolUse.ActivePattern ap ->
      let signature = getFuncSignature symbol.DisplayContext ap
      Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | SymbolUse.GenericParameter gp ->
      let signature =
        $"'%s{gp.Name} (requires %s{formatGenericParameter false symbol.DisplayContext gp})"

      Some((signature, EntityInfo.Empty), footerForType symbol, cn)

    | _ -> None


  /// Returns formatted symbol signature and footer that can be used to enhance standard FCS' text tooltips
  let getTooltipDetailsFromSymbol (symbol: FSharpSymbol) =
    let cn = compiledNameType' symbol

    match symbol with
    | EntityFromSymbol(fse, _) ->
      try
        let signature = getEntitySignature lastDisplayContext fse
        Some(signature, footerForType' symbol, cn)
      with _ ->
        None

    | Constructor func ->
      match func.EnclosingEntitySafe with
      | Some ent when ent.IsValueType || ent.IsEnum ->
        //ValueTypes
        let signature = getFuncSignature lastDisplayContext func
        Some((signature, EntityInfo.Empty), footerForType' symbol, cn)
      | _ ->
        //ReferenceType constructor
        let signature = getFuncSignature lastDisplayContext func
        Some((signature, EntityInfo.Empty), footerForType' symbol, cn)

    | SymbolPatterns.Operator func ->
      let signature = getFuncSignature lastDisplayContext func
      Some((signature, EntityInfo.Empty), footerForType' symbol, cn)

    | Property prop ->
      let signature = getFuncSignature lastDisplayContext prop
      Some((signature, EntityInfo.Empty), footerForType' symbol, cn)

    | ClosureOrNestedFunction func ->
      //represents a closure or nested function
      let signature = getFuncSignature lastDisplayContext func
      Some((signature, EntityInfo.Empty), footerForType' symbol, cn)

    | Function func ->
      let signature = getFuncSignature lastDisplayContext func
      Some((signature, EntityInfo.Empty), footerForType' symbol, cn)

    | Val func ->
      //val name : Type
      let signature = getValSignature lastDisplayContext func
      Some((signature, EntityInfo.Empty), footerForType' symbol, cn)

    | Field(fsf, _) ->
      let signature = getFieldSignature lastDisplayContext fsf
      Some((signature, EntityInfo.Empty), footerForType' symbol, cn)

    | UnionCase uc ->
      let signature = getUnionCaseSignature lastDisplayContext uc
      Some((signature, EntityInfo.Empty), footerForType' symbol, cn)

    | ActivePatternCase apc ->
      let signature = getAPCaseSignature lastDisplayContext apc
      Some((signature, EntityInfo.Empty), footerForType' symbol, cn)


    | GenericParameter gp ->
      let signature =
        $"'%s{gp.Name} (requires %s{formatGenericParameter false lastDisplayContext gp})"

      Some((signature, EntityInfo.Empty), footerForType' symbol, cn)

    | _ -> None
