[<AutoOpen>]
module internal FsAutoComplete.Utils

open System.Diagnostics
open System.Threading.Tasks
open System.IO
open System.Collections.Concurrent
open System
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Symbols
open System.Runtime.CompilerServices
open System.Globalization


/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
let dispose (d: #IDisposable) = d.Dispose()

/// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.</summary>
/// <returns>A task that represents the asynchronous dispose operation.</returns>
let disposeAsync (d: #IAsyncDisposable) = d.DisposeAsync()

module internal Map =
  /// Combine two maps of identical types by starting with the first map and overlaying the second one.
  /// Because map updates shadow, any keys in the second map will have priority.
  let merge (first: Map<'a, 'b>) (second: Map<'a, 'b>) =
    let mutable result = first

    for (KeyValue(key, value)) in second do
      result <- Map.add key value result

    result

  /// Combine two maps by taking the first value of each key found.
  let combineTakeFirst (first: Map<_, _>) (second: Map<_, _>) =
    let mutable result = first

    for (KeyValue(key, value)) in second do
      if result.ContainsKey key then
        ()
      else
        result <- Map.add key value result

    result

  let values (m: Map<_, _>) =
    seq {
      for (KeyValue(_, value)) in m do
        yield value
    }

module Seq =
  let intersperse separator (sequence: #seq<'a>) =
    seq {
      let mutable notFirst = false

      for element in sequence do
        if notFirst then
          yield separator

        yield element
        notFirst <- true
    }

module ProcessHelper =

  let WaitForExitAsync (p: Process) =
    async {
      let tcs =
        TaskCompletionSource<obj>(TaskCreationOptions.RunContinuationsAsynchronously)

      p.EnableRaisingEvents <- true
      p.Exited.Add(fun _args -> tcs.TrySetResult(null) |> ignore)

      let! token = Async.CancellationToken

      let _registered = token.Register(fun _ -> tcs.SetCanceled())

      let! _ = tcs.Task |> Async.AwaitTask
      ()
    }

type ResultOrString<'a> = Result<'a, string>

type Serializer = obj -> string
type ProjectFilePath = string
type SourceFilePath = string
type FilePath = string
type LineStr = string

/// OS-local, normalized path
[<Measure>]
type LocalPath

/// An HTTP url
[<Measure>]
type Url

/// OS-Sensitive path segment from some repository root
[<Measure>]
type RepoPathSegment
// OS-agnostic path segment from some repository root
[<Measure>]
type NormalizedRepoPathSegment

type Document =
  { FullName: string
    LineCount: int
    GetText: unit -> string
    GetLineText0: int -> string
    GetLineText1: int -> string }


/// <summary>
/// Checks if the file ends with `.fsx` `.fsscript` or `.sketchfs`
/// </summary>
let inline isAScript (fileName: ReadOnlySpan<char>) =
  fileName.EndsWith ".fsx"
  || fileName.EndsWith ".fsscript"
  || fileName.EndsWith ".sketchfs"

/// <summary>
/// Checks if the file ends with `.fsi`
/// </summary>
let inline isSignatureFile (fileName: ReadOnlySpan<char>) = fileName.EndsWith ".fsi"
let inline isSignatureFileStr (fileName: string) = fileName.EndsWith ".fsi"

/// <summary>
/// Checks if the file ends with `.fs`
/// </summary>
let isFsharpFile (fileName: ReadOnlySpan<char>) = fileName.EndsWith ".fs"

let inline internal isFileWithFSharpI fileName = isAScript fileName || isSignatureFile fileName || isFsharpFile fileName


/// <summary>
/// This is a combination of `isAScript`, `isSignatureFile`, and `isFsharpFile`
/// </summary>
/// <param name="fileName"></param>
/// <returns></returns>
let inline isFileWithFSharp (fileName: string) = isFileWithFSharpI (fileName.AsSpan())

let normalizePath (file: string) : string =
  if isFileWithFSharp file then
    let p = Path.GetFullPath file
    ((p.Chars 0).ToString().ToLower() + p.Substring(1))
  else
    file

let inline combinePaths path1 (path2: string) = Path.Combine(path1, path2.TrimStart [| '\\'; '/' |])

let inline (</>) path1 path2 = combinePaths path1 path2

let projectOptionsToParseOptions (checkOptions: FSharpProjectOptions) =
  //TODO: Investigate why sometimes SourceFiles are not filled
  let files =
    match checkOptions.SourceFiles with
    | [||] -> checkOptions.OtherOptions |> Array.where (isFileWithFSharp)
    | x -> x

  { FSharpParsingOptions.Default with
      SourceFiles = files }


[<RequireQualifiedAccess>]
module Option =

  let inline attempt (f: unit -> 'T) =
    try
      Some <| f ()
    with _ ->
      None

  /// ensure the condition is true before continuing
  let inline guard (b) = if b then Some() else None

[<RequireQualifiedAccess>]
module Result =
  let inline bimap okF errF r =
    match r with
    | Ok x -> okF x
    | Error y -> errF y

  let inline ofOption recover o =
    match o with
    | Some x -> Ok x
    | None -> Error(recover ())

  let inline ofVOption recover o =
    match o with
    | ValueSome x -> Ok x
    | ValueNone -> Error(recover ())

  /// ensure the condition is true before continuing
  let inline guard condition errorValue = if condition () then Ok() else Error errorValue

[<RequireQualifiedAccess>]
module Async =
  /// Transforms an Async value using the specified function.
  [<CompiledName("Map")>]
  let map (mapping: 'a -> 'b) (value: Async<'a>) : Async<'b> =
    async {
      // Get the input value.
      let! x = value
      // Apply the mapping function and return the result.
      return mapping x
    }

  // Transforms an Async value using the specified Async function.
  [<CompiledName("Bind")>]
  let bind (binding: 'a -> Async<'b>) (value: Async<'a>) : Async<'b> =
    async {
      // Get the input value.
      let! x = value
      // Apply the binding function and return the result.
      return! binding x
    }

  let StartCatchCancellation (work, cancellationToken) =
    Async.FromContinuations(fun (cont, econt, _) ->
      // When the child is cancelled, report OperationCancelled
      // as an ordinary exception to "error continuation" rather
      // than using "cancellation continuation"
      let ccont e = econt e
      // Start the workflow using a provided cancellation token
      Async.StartWithContinuations(work, cont, econt, ccont, cancellationToken = cancellationToken))

  /// <summary>Creates an asynchronous computation that executes all the given asynchronous computations, using 75% of the Environment.ProcessorCount</summary>
  /// <param name="computations">A sequence of distinct computations to be parallelized.</param>
  let parallel75 computations =
    let maxConcurrency =
      Math.Max(1.0, Math.Floor((float System.Environment.ProcessorCount) * 0.75))

    Async.Parallel(computations, int maxConcurrency)

  [<RequireQualifiedAccess>]
  module Array =
    /// Async implementation of Array.map.
    let map (mapping: 'T -> Async<'U>) (array: 'T[]) : Async<'U[]> =
      let len = Array.length array
      let result = Array.zeroCreate len

      async {
        for i in 0 .. len - 1 do
          let! mappedValue = mapping array.[i]
          result.[i] <- mappedValue

        // Return the completed results.
        return result
      }

[<RequireQualifiedAccess>]
module AsyncResult =
  let inline bimap okF errF r = Async.map (Result.bimap okF errF) r
  let inline ofOption recover o = Async.map (Result.ofOption recover) o


[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Array =
  let inline private checkNonNull argName arg =
    match box arg with
    | null -> nullArg argName
    | _ -> ()

  /// Optimized arrays equality. ~100x faster than `array1 = array2` on strings.
  /// ~2x faster for floats
  /// ~0.8x slower for ints
  let inline areEqual (xs: 'T[]) (ys: 'T[]) =
    match xs, ys with
    | null, null -> true
    | [||], [||] -> true
    | null, _
    | _, null -> false
    | _ when xs.Length <> ys.Length -> false
    | _ ->
      let mutable break' = false
      let mutable i = 0
      let mutable result = true

      while i < xs.Length && not break' do
        if xs.[i] <> ys.[i] then
          break' <- true
          result <- false

        i <- i + 1

      result


  /// Fold over the array passing the index and element at that index to a folding function
  let foldi (folder: 'State -> int -> 'T -> 'State) (state: 'State) (array: 'T[]) =
    checkNonNull "array" array

    if array.Length = 0 then
      state
    else
      let folder = OptimizedClosures.FSharpFunc<_, _, _, _>.Adapt folder

      let mutable state: 'State = state
      let len = array.Length

      for i = 0 to len - 1 do
        state <- folder.Invoke(state, i, array.[i])

      state

  /// Returns all heads of a given array.
  /// For [|1;2;3|] it returns [|[|1; 2; 3|]; [|1; 2|]; [|1|]|]
  let heads (array: 'T[]) =
    checkNonNull "array" array
    let res = Array.zeroCreate<'T[]> array.Length

    for i = array.Length - 1 downto 0 do
      res.[i] <- array.[0..i]

    res

  /// check if subArray is found in the wholeArray starting
  /// at the provided index
  let inline isSubArray (subArray: 'T[]) (wholeArray: 'T[]) index =
    if isNull subArray || isNull wholeArray then
      false
    elif subArray.Length = 0 then
      true
    elif subArray.Length > wholeArray.Length then
      false
    elif subArray.Length = wholeArray.Length then
      areEqual subArray wholeArray
    else
      let rec loop subidx idx =
        if subidx = subArray.Length then
          true
        elif subArray.[subidx] = wholeArray.[idx] then
          loop (subidx + 1) (idx + 1)
        else
          false

      loop 0 index

  /// Returns true if one array has another as its subset from index 0.
  let startsWith (prefix: _[]) (whole: _[]) = isSubArray prefix whole 0

  /// Returns true if one array has trailing elements equal to another's.
  let endsWith (suffix: _[]) (whole: _[]) = isSubArray suffix whole (whole.Length - suffix.Length)

  /// Returns a new array with an element replaced with a given value.
  let replace index value (array: _[]) =
    checkNonNull "array" array

    if index >= array.Length then
      raise (IndexOutOfRangeException "index")

    let res = Array.copy array
    res.[index] <- value
    res

  /// pass an array byref to reverse it in place
  let revInPlace (array: 'T[]) =
    checkNonNull "array" array

    if areEqual array [||] then
      ()
    else
      let arrLen, revLen = array.Length - 1, array.Length / 2 - 1

      for idx in 0..revLen do
        let t1 = array.[idx]
        let t2 = array.[arrLen - idx]
        array.[idx] <- t2
        array.[arrLen - idx] <- t1

  let splitAt (n: int) (xs: 'a[]) : 'a[] * 'a[] =
    match xs with
    | [||]
    | [| _ |] -> xs, [||]
    | _ when n >= xs.Length || n < 0 -> xs, [||]
    | _ -> xs.[0 .. n - 1], xs.[n..]

  let partitionResults (xs: _[]) =
    let oks = ResizeArray(xs.Length)
    let errors = ResizeArray(xs.Length)

    for x in xs do
      match x with
      | Ok ok -> oks.Add ok
      | Error err -> errors.Add err

    oks.ToArray(), errors.ToArray()

module List =

  ///Returns the greatest of all elements in the list that is less than the threshold
  let maxUnderThreshold nmax = List.maxBy (fun n -> if n > nmax then 0 else n)

  /// Groups a tupled list by the first item to produce a list of values
  let groupByFst (tupledItems: ('Key * 'Value) list) =
    tupledItems
    |> List.groupBy (fst)
    |> List.map (fun (key, list) -> key, list |> List.map snd)

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module String =
  /// Concatenates all the elements of a string array, using the specified separator between each element.
  let inline join (separator: string) (items: string seq) = String.Join(separator, items)

  let inline toCharArray (str: string) = str.ToCharArray()

  let lowerCaseFirstChar (str: string) =
    if String.IsNullOrEmpty str || Char.IsLower(str, 0) then
      str
    else
      let strArr = toCharArray str

      match Array.tryHead strArr with
      | None -> str
      | Some c ->
        strArr.[0] <- Char.ToLower c
        String(strArr)


  let extractTrailingIndex (str: string) =
    match str with
    | null -> null, None
    | _ ->
      let charr = str.ToCharArray()
      Array.revInPlace charr
      let digits = Array.takeWhile Char.IsDigit charr
      Array.revInPlace digits

      String digits
      |> function
        | "" -> str, None
        | index -> str.Substring(0, str.Length - index.Length), Some(int index)


  let (|StartsWith|_|) (pattern: string) (value: string) =
    if String.IsNullOrWhiteSpace value then
      None
    elif value.StartsWith(pattern, StringComparison.Ordinal) then
      Some()
    else
      None

  let split (splitter: char) (s: string) =
    s.Split([| splitter |], StringSplitOptions.RemoveEmptyEntries) |> List.ofArray

  let getLines (str: string) =
    use reader = new StringReader(str)

    [| let line = ref (reader.ReadLine())

       while not (isNull (line.Value)) do
         yield line.Value
         line.Value <- reader.ReadLine()

       if str.EndsWith("\n", StringComparison.Ordinal) then
         // last trailing space not returned
         // http://stackoverflow.com/questions/19365404/stringreader-omits-trailing-linebreak
         yield String.Empty |]

  type SplitResult =
    | NoMatch
    | Split of left: string * right: string

  let splitAtChar (splitter: char) (s: string) =
    match s.IndexOf splitter with
    | -1 -> NoMatch
    | n -> Split(s.[0 .. n - 1], s.Substring(n + 1))

[<Extension>]
type ReadOnlySpanExtensions =
  /// Note: empty string -> 1 line
  [<Extension>]
  static member CountLines(text: ReadOnlySpan<char>) =
    let mutable count = 0

    for _ in text.EnumerateLines() do
      count <- count + 1

    count

  [<Extension>]
  static member LastLine(text: ReadOnlySpan<char>) =
    let mutable line = ReadOnlySpan.Empty

    for current in text.EnumerateLines() do
      line <- current

    line

#if !NET7_0_OR_GREATER
  [<Extension>]
  static member IndexOfAnyExcept(span: ReadOnlySpan<char>, value0: char, value1: char) =
    let mutable i = 0
    let mutable found = false

    while not found && i < span.Length do
      let c = span[i]

      if c <> value0 && c <> value1 then
        found <- true
      else
        i <- i + 1

    if found then i else -1

  [<Extension>]
  static member IndexOfAnyExcept(span: ReadOnlySpan<char>, values: ReadOnlySpan<char>) =
    let mutable i = 0
    let mutable found = false

    while not found && i < span.Length do
      if values.IndexOf span[i] < 0 then
        found <- true
      else
        i <- i + 1

    if found then i else -1

  [<Extension>]
  static member LastIndexOfAnyExcept(span: ReadOnlySpan<char>, value0: char, value1: char) =
    let mutable i = span.Length - 1
    let mutable found = false

    while not found && i >= 0 do
      let c = span[i]

      if c <> value0 && c <> value1 then
        found <- true
      else
        i <- i - 1

    if found then i else -1
#endif

type ConcurrentDictionary<'key, 'value> with

  member x.TryFind key =
    match x.TryGetValue key with
    | true, value -> Some value
    | _ -> None

type Path with

  static member GetFullPathSafe(path: string) =
    try
      Path.GetFullPath path
    with _ ->
      path

  static member GetFileNameSafe(path: string) =
    try
      Path.GetFileName path
    with _ ->
      path

  //static member LocalPathToUri(filePath: string<LocalPath>) = Path.FilePathToUri(UMX.untag filePath)

  /// Algorithm from https://stackoverflow.com/a/35734486/433393 for converting file paths to uris,
  /// modified slightly to not rely on the System.Path members because they vary per-platform
  static member FilePathToUri(filePath: string) : string =
    let filePath, finished =
      if filePath.Contains "Untitled-" then
        let rg = System.Text.RegularExpressions.Regex.Match(filePath, @"(Untitled-\d+).fsx")

        if rg.Success then
          rg.Groups.[1].Value, true
        else
          filePath, false
      else
        filePath, false

    if not finished then
      let uri = System.Text.StringBuilder(filePath.Length)

      for c in filePath do
        if
          (c >= 'a' && c <= 'z')
          || (c >= 'A' && c <= 'Z')
          || (c >= '0' && c <= '9')
          || c = '+'
          || c = '/'
          || c = '.'
          || c = '-'
          || c = '_'
          || c = '~'
          || c > '\xFF'
        then
          uri.Append(c) |> ignore
        // handle windows path separator chars.
        // we _would_ use Path.DirectorySeparator/AltDirectorySeparator, but those vary per-platform and we want this
        // logic to work cross-platform (for tests)
        else if c = '\\' then
          uri.Append('/') |> ignore
        else
          uri.Append('%') |> ignore
          uri.Append((int c).ToString("X2")) |> ignore

      if uri.Length >= 2 && uri.[0] = '/' && uri.[1] = '/' then // UNC path
        "file:" + uri.ToString()
      else
        "file:///" + (uri.ToString()).TrimStart('/')
    // handle windows path separator chars.
    // we _would_ use Path.DirectorySeparator/AltDirectorySeparator, but those vary per-platform and we want this
    // logic to work cross-platform (for tests)
    else
      "untitled:" + filePath

  /// handles unifying the local-path logic for windows and non-windows paths,
  /// without doing a check based on what the current system's OS is.
  static member FileUriToLocalPath(uriString: string) =
    /// a test that checks if the start of the line is a windows-style drive string, for example
    /// /d:, /c:, /z:, etc.
    let isWindowsStyleDriveLetterMatch (s: string) =
      match s.[0..2].ToCharArray() with
      | [||]
      | [| _ |]
      | [| _; _ |] -> false
      // 26 windows drive letters allowed, only
      | [| '/'; driveLetter; ':' |] when Char.IsLetter driveLetter -> true
      | _ -> false

    let initialLocalPath = Uri(uriString).LocalPath

    let fn =
      if isWindowsStyleDriveLetterMatch initialLocalPath then
        let trimmed = initialLocalPath.TrimStart('/')

        let initialDriveLetterCaps =
          string (System.Char.ToLower trimmed.[0]) + trimmed.[1..]

        initialDriveLetterCaps
      else
        initialLocalPath

    if uriString.StartsWith "untitled:" then
      (fn + ".fsx")
    else
      fn

let inline debug msg = Printf.kprintf Debug.WriteLine msg
let inline fail msg = Printf.kprintf Debug.Fail msg


let chooseByPrefix (prefix: string) (s: string) =
  if s.StartsWith(prefix, StringComparison.Ordinal) then
    Some(s.Substring(prefix.Length))
  else
    None

let chooseByPrefix2 prefixes (s: string) = prefixes |> List.tryPick (fun prefix -> chooseByPrefix prefix s)

let splitByPrefix (prefix: string) (s: string) =
  if s.StartsWith(prefix, StringComparison.Ordinal) then
    Some(prefix, s.Substring(prefix.Length))
  else
    None

let splitByPrefix2 prefixes (s: string) = prefixes |> List.tryPick (fun prefix -> splitByPrefix prefix s)

[<AutoOpen>]
module Patterns =

  let (|StartsWith|_|) (pat: string) (str: string) =
    match str with
    | null -> None
    | _ when str.StartsWith(pat, StringComparison.Ordinal) -> Some str
    | _ -> None

  let (|Contains|_|) (pat: string) (str: string) =
    match str with
    | null -> None
    | _ when str.Contains pat -> Some str
    | _ -> None


module Version =

  open System.Reflection

  type VersionInfo = { Version: string; GitSha: string }

  let private informationalVersion () =
    let assemblies =
      typeof<VersionInfo>.Assembly.GetCustomAttributes(typeof<AssemblyInformationalVersionAttribute>, true)

    match assemblies with
    | [| x |] ->
      let assembly = x :?> AssemblyInformationalVersionAttribute

      assembly.InformationalVersion
    | _ -> ""

  let info () =
    // it's in the format VERSION+GITSHA
    // like 1.2.3+01989f5dc405661d639d48ee1d1804c0b331ca63

    let v = informationalVersion ()

    let version, sha =
      match v.Split [| '+' |] with
      | [| v; sha |] -> v, sha
      | [| v |] -> v, ""
      | _ -> "", ""

    { VersionInfo.Version = version
      GitSha = sha }

//source: https://nbevans.wordpress.com/2014/08/09/a-simple-stereotypical-javascript-like-debounce-service-for-f/
type Debounce<'a>(timeout, fn) as x =

  let mailbox =
    MailboxProcessor<'a>.Start(fun agent ->
      let rec loop ida idb arg =
        async {
          let! r = agent.TryReceive(x.Timeout)

          match r with
          | Some arg -> return! loop ida (idb + 1) (Some arg)
          | None when ida <> idb ->
            do! fn arg.Value
            return! loop 0 0 None
          | None -> return! loop 0 0 arg
        }

      loop 0 0 None)

  /// Calls the function, after debouncing has been applied.
  member _.Bounce(arg) = mailbox.Post(arg)

  /// Timeout in ms
  member val Timeout = timeout with get, set

module Indentation =
  let inline get (line: string) = line.Length - line.AsSpan().Trim(' ').Length


type FSharpSymbol with

  member inline x.XDoc =
    match x with
    | :? FSharpEntity as e -> e.XmlDoc
    | :? FSharpUnionCase as u -> u.XmlDoc
    | :? FSharpField as f -> f.XmlDoc
    | :? FSharpActivePatternCase as c -> c.XmlDoc
    | :? FSharpGenericParameter as g -> g.XmlDoc
    | :? FSharpMemberOrFunctionOrValue as m -> m.XmlDoc
    | :? FSharpStaticParameter
    | :? FSharpParameter -> FSharpXmlDoc.None
    | _ -> failwith $"cannot fetch xmldoc for unknown FSharpSymbol subtype {x.GetType().FullName}"

  member inline x.XSig =
    match x with
    | :? FSharpEntity as e -> e.XmlDocSig
    | :? FSharpUnionCase as u -> u.XmlDocSig
    | :? FSharpField as f -> f.XmlDocSig
    | :? FSharpActivePatternCase as c -> c.XmlDocSig
    | :? FSharpMemberOrFunctionOrValue as m -> m.XmlDocSig
    | :? FSharpGenericParameter
    | :? FSharpStaticParameter
    | :? FSharpParameter -> ""
    | _ -> failwith $"cannot fetch XmlDocSig for unknown FSharpSymbol subtype {x.GetType().FullName}"

  member inline x.DefinitionRange =
    match x with
    | :? FSharpEntity as e -> e.DeclarationLocation
    | :? FSharpUnionCase as u -> u.DeclarationLocation
    | :? FSharpField as f -> f.DeclarationLocation
    | :? FSharpActivePatternCase as c -> c.DeclarationLocation
    | :? FSharpGenericParameter as g -> g.DeclarationLocation
    | :? FSharpMemberOrFunctionOrValue as m -> m.DeclarationLocation
    | :? FSharpStaticParameter as s -> s.DeclarationLocation
    | :? FSharpParameter as p -> p.DeclarationLocation
    | _ -> failwith $"cannot fetch DefinitionRange for unknown FSharpSymbol subtype {x.GetType().FullName}"
