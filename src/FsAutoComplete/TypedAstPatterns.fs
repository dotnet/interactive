[<AutoOpen>]
module FsAutoComplete.Patterns

open FSharp.Compiler.SourceCodeServices
open System


/// Active patterns over `FSharpSymbolUse`.
module SymbolUse =

    let (|ActivePatternCase|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpActivePatternCase as ap-> ActivePatternCase(ap) |> Some
        | _ -> None

    let private attributeSuffixLength = "Attribute".Length

    let (|Entity|_|) (symbol : FSharpSymbolUse) : (FSharpEntity * (* cleanFullNames *) string list) option =
        match symbol.Symbol with
        | :? FSharpEntity as ent ->
            // strip generic parameters count suffix (List`1 => List)
            let cleanFullName =
                // `TryFullName` for type aliases is always `None`, so we have to make one by our own
                if ent.IsFSharpAbbreviation then
                    [ent.AccessPath + "." + ent.DisplayName]
                else
                    ent.TryFullName
                    |> Option.toList
                    |> List.map (fun fullName ->
                        if ent.GenericParameters.Count > 0 && fullName.Length > 2 then
                            fullName.[0..fullName.Length - 3] //Get name without sufix specifing number of generic arguments (for example `'2`)
                        else fullName)

            let cleanFullNames =
                cleanFullName
                |> List.collect (fun cleanFullName ->
                    if ent.IsAttributeType then
                        [cleanFullName; cleanFullName.[0..cleanFullName.Length - attributeSuffixLength - 1]] //Get full name, and name without AttributeSuffix (for example `Literal` instead of `LiteralAttribute`)
                    else [cleanFullName]
                    )
            Some (ent, cleanFullNames)
        | _ -> None


    let (|Field|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpField as field-> Some field
        |  _ -> None

    let (|GenericParameter|_|) (symbol: FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpGenericParameter as gp -> Some gp
        | _ -> None

    let (|MemberFunctionOrValue|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpMemberOrFunctionOrValue as func -> Some func
        | _ -> None

    let (|ActivePattern|_|) = function
        | MemberFunctionOrValue m when m.IsActivePattern -> Some m | _ -> None

    let (|Parameter|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpParameter as param -> Some param
        | _ -> None

    let (|StaticParameter|_|) (symbol : FSharpSymbolUse) =
        #if NO_EXTENSIONTYPING
        Some
        #else
        match symbol.Symbol with
        | :? FSharpStaticParameter as sp -> Some sp
        | _ -> None
        #endif

    let (|UnionCase|_|) (symbol : FSharpSymbolUse) =
        match symbol.Symbol with
        | :? FSharpUnionCase as uc-> Some uc
        | _ -> None

    let (|Constructor|_|) = function
        | MemberFunctionOrValue func when func.IsConstructor || func.IsImplicitConstructor -> Some func
        | _ -> None


    let (|TypeAbbreviation|_|) = function
        | Entity (entity, _) when entity.IsFSharpAbbreviation -> Some entity
        | _ -> None

    let (|Class|_|) = function
        | Entity (entity, _) when entity.IsClass -> Some entity
        | Entity (entity, _) when entity.IsFSharp &&
            entity.IsOpaque &&
            not entity.IsFSharpModule &&
            not entity.IsNamespace &&
            not entity.IsDelegate &&
            not entity.IsFSharpUnion &&
            not entity.IsFSharpRecord &&
            not entity.IsInterface &&
            not entity.IsValueType -> Some entity
        | _ -> None

    let (|Delegate|_|) = function
        | Entity (entity, _) when entity.IsDelegate -> Some entity
        | _ -> None

    let (|Event|_|) = function
        | MemberFunctionOrValue symbol when symbol.IsEvent -> Some symbol
        | _ -> None

    let (|Property|_|) = function
        | MemberFunctionOrValue symbol when symbol.IsProperty || symbol.IsPropertyGetterMethod || symbol.IsPropertySetterMethod -> Some symbol
        | _ -> None

    let inline private notCtorOrProp (symbol:FSharpMemberOrFunctionOrValue) =
        not symbol.IsConstructor && not symbol.IsPropertyGetterMethod && not symbol.IsPropertySetterMethod

    let (|Method|_|) (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | MemberFunctionOrValue symbol when
            symbol.IsModuleValueOrMember  &&
            not symbolUse.IsFromPattern &&
            not symbol.IsOperatorOrActivePattern &&
            not symbol.IsPropertyGetterMethod &&
            not symbol.IsPropertySetterMethod -> Some symbol
        | _ -> None

    let (|Function|_|) (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | MemberFunctionOrValue symbol when
            notCtorOrProp symbol  &&
            symbol.IsModuleValueOrMember &&
            not symbol.IsOperatorOrActivePattern &&
            not symbolUse.IsFromPattern ->

            match symbol.FullTypeSafe with
            | Some fullType when fullType.IsFunctionType -> Some symbol
            | _ -> None
        | _ -> None

    let (|Operator|_|) (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | MemberFunctionOrValue symbol when
            notCtorOrProp symbol &&
            not symbolUse.IsFromPattern &&
            not symbol.IsActivePattern &&
            symbol.IsOperatorOrActivePattern ->

            match symbol.FullTypeSafe with
            | Some fullType when fullType.IsFunctionType -> Some symbol
            | _ -> None
        | _ -> None

    let (|Pattern|_|) (symbolUse:FSharpSymbolUse) =
        match symbolUse with
        | MemberFunctionOrValue symbol when
            notCtorOrProp symbol &&
            not symbol.IsOperatorOrActivePattern &&
            symbolUse.IsFromPattern ->

            match symbol.FullTypeSafe with
            | Some fullType when fullType.IsFunctionType ->Some symbol
            | _ -> None
        | _ -> None


    let (|ClosureOrNestedFunction|_|) = function
        | MemberFunctionOrValue symbol when
            notCtorOrProp symbol &&
            not symbol.IsOperatorOrActivePattern &&
            not symbol.IsModuleValueOrMember ->

            match symbol.FullTypeSafe with
            | Some fullType when fullType.IsFunctionType -> Some symbol
            | _ -> None
        | _ -> None


    let (|Val|_|) = function
        | MemberFunctionOrValue symbol when notCtorOrProp symbol &&
                                            not symbol.IsOperatorOrActivePattern ->
            match symbol.FullTypeSafe with
            | Some _fullType -> Some symbol
            | _ -> None
        | _ -> None

    let (|Enum|_|) = function
        | Entity (entity, _) when entity.IsEnum -> Some entity
        | _ -> None

    let (|Interface|_|) = function
        | Entity (entity, _) when entity.IsInterface -> Some entity
        | _ -> None

    let (|Module|_|) = function
        | Entity (entity, _) when entity.IsFSharpModule -> Some entity
        | _ -> None

    let (|Namespace|_|) = function
        | Entity (entity, _) when entity.IsNamespace -> Some entity
        | _ -> None

    let (|Record|_|) = function
        | Entity (entity, _) when entity.IsFSharpRecord -> Some entity
        | _ -> None

    let (|Union|_|) = function
        | Entity (entity, _) when entity.IsFSharpUnion -> Some entity
        | _ -> None

    let (|ValueType|_|) = function
        | Entity (entity, _) when entity.IsValueType && not entity.IsEnum -> Some entity
        | _ -> None

    let (|ComputationExpression|_|) (symbol:FSharpSymbolUse) =
        if symbol.IsFromComputationExpression then Some symbol
        else None

    let (|Attribute|_|) = function
        | Entity (entity, _) when entity.IsAttributeType -> Some entity
        | _ -> None

[<AutoOpen>]
/// Active patterns over `FSharpSymbol`.
module SymbolPatterns =

    let private attributeSuffixLength = "Attribute".Length

    let (|Entity|_|) (symbol : FSharpSymbolUse) : (FSharpEntity * (* cleanFullNames *) string list) option =
        match symbol.Symbol with
        | :? FSharpEntity as ent ->
            // strip generic parameters count suffix (List`1 => List)
            let cleanFullName =
                // `TryFullName` for type aliases is always `None`, so we have to make one by our own
                if ent.IsFSharpAbbreviation then
                    [ent.AccessPath + "." + ent.DisplayName]
                else
                    ent.TryFullName
                    |> Option.toList
                    |> List.map (fun fullName ->
                        if ent.GenericParameters.Count > 0 && fullName.Length > 2 then
                            fullName.[0..fullName.Length - 3] //Get name without sufix specifing number of generic arguments (for example `'2`)
                        else fullName)

            let cleanFullNames =
                cleanFullName
                |> List.collect (fun cleanFullName ->
                    if ent.IsAttributeType then
                        [cleanFullName; cleanFullName.[0..cleanFullName.Length - attributeSuffixLength - 1]] //Get full name, and name without AttributeSuffix (for example `Literal` instead of `LiteralAttribute`)
                    else [cleanFullName]
                    )
            Some (ent, cleanFullNames)
        | _ -> None

    let (|EntityFromSymbol|_|) (symbol : FSharpSymbol) : (FSharpEntity * (* cleanFullNames *) string list) option =
        match symbol with
        | :? FSharpEntity as ent ->
            // strip generic parameters count suffix (List`1 => List)
            let cleanFullName =
                // `TryFullName` for type aliases is always `None`, so we have to make one by our own
                if ent.IsFSharpAbbreviation then
                    [ent.AccessPath + "." + ent.DisplayName]
                else
                    ent.TryFullName
                    |> Option.toList
                    |> List.map (fun fullName ->
                        if ent.GenericParameters.Count > 0 && fullName.Length > 2 then
                            fullName.[0..fullName.Length - 3] //Get name without sufix specifing number of generic arguments (for example `'2`)
                        else fullName)

            let cleanFullNames =
                cleanFullName
                |> List.collect (fun cleanFullName ->
                    if ent.IsAttributeType then
                        [cleanFullName; cleanFullName.[0..cleanFullName.Length - attributeSuffixLength - 1]] //Get full name, and name without AttributeSuffix (for example `Literal` instead of `LiteralAttribute`)
                    else [cleanFullName]
                    )
            Some (ent, cleanFullNames)
        | _ -> None

    let (|AbbreviatedType|_|) (entity: FSharpEntity) =
        if entity.IsFSharpAbbreviation then Some entity.AbbreviatedType
        else None

    let (|TypeWithDefinition|_|) (ty: FSharpType) =
        if ty.HasTypeDefinition then Some ty.TypeDefinition
        else None

    let rec getEntityAbbreviatedType (entity: FSharpEntity) =
        if entity.IsFSharpAbbreviation then
            match entity.AbbreviatedType with
            | TypeWithDefinition def -> getEntityAbbreviatedType def
            | abbreviatedType -> entity, Some abbreviatedType
        else entity, None

    let rec getAbbreviatedType (fsharpType: FSharpType) =
        if fsharpType.IsAbbreviation then
            getAbbreviatedType fsharpType.AbbreviatedType
        else fsharpType

    let (|Attribute|_|) (entity: FSharpEntity) =
        let isAttribute (entity: FSharpEntity) =
            let getBaseType (entity: FSharpEntity) =
                try
                    match entity.BaseType with
                    | Some (TypeWithDefinition def) -> Some def
                    | _ -> None
                with _ -> None

            let rec isAttributeType (ty: FSharpEntity option) =
                match ty with
                | None -> false
                | Some ty ->
                    match ty.TryGetFullName() with
                    | None -> false
                    | Some fullName ->
                        fullName = "System.Attribute" || isAttributeType (getBaseType ty)
            isAttributeType (Some entity)
        if isAttribute entity then Some() else None

    let (|ValueType|_|) (e: FSharpEntity) =
        if e.IsEnum || e.IsValueType || hasAttribute<MeasureAnnotatedAbbreviationAttribute> e.Attributes then Some()
        else None

    let (|Class|_|) (original: FSharpEntity, abbreviated: FSharpEntity, _) =
        if abbreviated.IsClass
#if NO_EXTENSIONTYPING
            && original.IsFSharpAbbreviation then Some()
#else
            && (not abbreviated.IsStaticInstantiation || original.IsFSharpAbbreviation) then Some()
#endif
        else None

    let (|Record|_|) (e: FSharpEntity) = if e.IsFSharpRecord then Some() else None
    let (|UnionType|_|) (e: FSharpEntity) = if e.IsFSharpUnion then Some() else None
    let (|Delegate|_|) (e: FSharpEntity) = if e.IsDelegate then Some() else None
    let (|FSharpException|_|) (e: FSharpEntity) = if e.IsFSharpExceptionDeclaration then Some() else None
    let (|Interface|_|) (e: FSharpEntity) = if e.IsInterface then Some() else None
    let (|AbstractClass|_|) (e: FSharpEntity) =
        if hasAttribute<AbstractClassAttribute> e.Attributes then Some() else None

    let (|FSharpType|_|) (e: FSharpEntity) =
        if e.IsDelegate || e.IsFSharpExceptionDeclaration || e.IsFSharpRecord || e.IsFSharpUnion
            || e.IsInterface || e.IsMeasure
            || (e.IsFSharp && e.IsOpaque && not e.IsFSharpModule && not e.IsNamespace) then Some()
        else None

    let (|ProvidedType|_|) (e: FSharpEntity) =
#if NO_EXTENSIONTYPING
        None
#else
        if (e.IsProvided || e.IsProvidedAndErased || e.IsProvidedAndGenerated) && e.CompiledName = e.DisplayName then
            Some()
        else None
#endif

    let (|ByRef|_|) (e: FSharpEntity) = if e.IsByRef then Some() else None
    let (|Array|_|) (e: FSharpEntity) = if e.IsArrayType then Some() else None
    let (|FSharpModule|_|) (entity: FSharpEntity) = if entity.IsFSharpModule then Some() else None

    let (|Namespace|_|) (entity: FSharpEntity) = if entity.IsNamespace then Some() else None
    let (|ProvidedAndErasedType|_|) (entity: FSharpEntity) =
#if NO_EXTENSIONTYPING
        None
#else
        if entity.IsProvidedAndErased then Some() else None
#endif

    let (|Enum|_|) (entity: FSharpEntity) = if entity.IsEnum then Some() else None

    let (|Tuple|_|) (ty: FSharpType option) =
        ty |> Option.bind (fun ty -> if ty.IsTupleType then Some() else None)

    let (|RefCell|_|) (ty: FSharpType) =
        match getAbbreviatedType ty with
        | TypeWithDefinition def when
            def.IsFSharpRecord && def.FullName = "Microsoft.FSharp.Core.FSharpRef`1" -> Some()
        | _ -> None

    let (|FunctionType|_|) (ty: FSharpType) =
        if ty.IsFunctionType then Some()
        else None

    let (|Pattern|_|) (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpUnionCase
        | :? FSharpActivePatternCase -> Some()
        | _ -> None

    /// Field (field, fieldAbbreviatedType)
    let (|Field|_|) (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpField as field -> Some (field, getAbbreviatedType field.FieldType)
        | _ -> None

    let (|MutableVar|_|) (symbol: FSharpSymbol) =
        let isMutable =
            match symbol with
            | :? FSharpField as field -> field.IsMutable && not field.IsLiteral
            | :? FSharpMemberOrFunctionOrValue as func -> func.IsMutable
            | _ -> false
        if isMutable then Some() else None

    /// Entity (originalEntity, abbreviatedEntity, abbreviatedType)
    let (|FSharpEntity|_|) (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpEntity as entity ->
            let abbreviatedEntity, abbreviatedType = getEntityAbbreviatedType entity
            Some (entity, abbreviatedEntity, abbreviatedType)
        | _ -> None

    let (|Parameter|_|) (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpParameter -> Some()
        | _ -> None

    let (|UnionCase|_|) (e: FSharpSymbol) =
        match e with
        | :? FSharpUnionCase as uc -> Some uc
        | _ -> None

    let (|RecordField|_|) (e: FSharpSymbol) =
        match e with
        | :? FSharpField as field ->
            match field.DeclaringEntity with
            | Some entity when entity.IsFSharpRecord -> Some field
            | Some _
            | None -> None
        | _ -> None

    let (|ActivePatternCase|_|) (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpActivePatternCase as case -> Some case
        | _ -> None

    /// Func (memberFunctionOrValue, fullType)
    let (|MemberFunctionOrValue|_|) (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpMemberOrFunctionOrValue as func -> Some func
        | _ -> None

    /// Constructor (enclosingEntity)
    let (|Constructor|_|) = function
    | MemberFunctionOrValue func when func.IsConstructor || func.IsImplicitConstructor -> Some func
    | _ -> None

    let (|Function|_|) (symbol: FSharpSymbol) =
        match symbol with
        | MemberFunctionOrValue func ->
            match func.FullTypeSafe |> Option.map getAbbreviatedType with
            | Some typ when typ.IsFunctionType
                        && not func.IsPropertyGetterMethod
                        && not func.IsPropertySetterMethod
                        && not (isOperator func.DisplayName) -> Some func
            | _ -> None
        | _ -> None

    let (|ExtensionMember|_|) (func: FSharpMemberOrFunctionOrValue) =
        if func.IsExtensionMember then Some() else None

    let (|Event|_|) (func: FSharpMemberOrFunctionOrValue) =
        if func.IsEvent then Some () else None

    let inline private notCtorOrProp (symbol:FSharpMemberOrFunctionOrValue) =
        not symbol.IsConstructor && not symbol.IsPropertyGetterMethod && not symbol.IsPropertySetterMethod

    let (|Operator|_|) (symbolUse:FSharpSymbol) =
        match symbolUse with
        | MemberFunctionOrValue symbol when
            notCtorOrProp symbol &&
            not symbol.IsActivePattern &&
            symbol.IsOperatorOrActivePattern ->

            match symbol.FullTypeSafe with
            | Some fullType when fullType.IsFunctionType -> Some symbol
            | _ -> None
        | _ -> None

    let (|Property|_|) = function
    | MemberFunctionOrValue symbol when symbol.IsProperty || symbol.IsPropertyGetterMethod || symbol.IsPropertySetterMethod -> Some symbol
    | _ -> None

    let (|ClosureOrNestedFunction|_|) = function
        | MemberFunctionOrValue symbol when
            notCtorOrProp symbol &&
            not symbol.IsOperatorOrActivePattern &&
            not symbol.IsModuleValueOrMember ->

            match symbol.FullTypeSafe with
            | Some fullType when fullType.IsFunctionType -> Some symbol
            | _ -> None
        | _ -> None

    let (|Val|_|) = function
        | MemberFunctionOrValue symbol when notCtorOrProp symbol &&
                                            not symbol.IsOperatorOrActivePattern ->
            match symbol.FullTypeSafe with
            | Some _fullType -> Some symbol
            | _ -> None
        | _ -> None

    let (|GenericParameter|_|) (symbol: FSharpSymbol) =
        match symbol with
        | :? FSharpGenericParameter as gp -> Some gp
        | _ -> None