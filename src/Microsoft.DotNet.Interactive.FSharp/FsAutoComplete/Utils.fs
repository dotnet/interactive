[<AutoOpen>]
module internal FsAutoComplete.Utils

module internal Map =
    /// Combine two maps of identical types by starting with the first map and overlaying the second one.
    /// Because map updates shadow, any keys in the second map will have priority.
    let merge (first: Map<'a, 'b>) (second: Map<'a, 'b>) =
        let mutable result = first
        for (KeyValue(key, value)) in second do
            result <- Map.add key value result
        result

    /// Combine two maps by taking the first value of each key found.
    let combineTakeFirst (first: Map<_,_>) (second: Map<_,_>) =
        let mutable result = first
        for (KeyValue(key, value)) in second do
            if result.ContainsKey key
            then ()
            else result <- Map.add key value result
        result

    let values (m: Map<_, _>) =
        seq {
            for (KeyValue(_,value)) in m do
                yield value
        }

open System.Diagnostics
open System.Threading.Tasks

module internal ProcessHelper =
    let WaitForExitAsync(p: Process) = async {
        let tcs = new TaskCompletionSource<obj>()
        p.EnableRaisingEvents <- true
        p.Exited.Add(fun _args -> tcs.TrySetResult(null) |> ignore)

        let! token = Async.CancellationToken
        let _registered = token.Register(fun _ -> tcs.SetCanceled())
        let! _ = tcs.Task |> Async.AwaitTask
        ()
    }

open System.IO
open System.Collections.Concurrent
open System
open FSharp.Compiler.SourceCodeServices

type internal ResultOrString<'a> = Result<'a, string>


type internal Document =
    { FullName : string
      LineCount : int
      GetText : unit -> string
      GetLineText0 : int -> string
      GetLineText1 : int -> string}


type internal Serializer = obj -> string
type internal ProjectFilePath = string
type internal SourceFilePath = string
type internal FilePath = string
type internal LineStr = string

let isAScript (fileName: string) =
    let ext = Path.GetExtension(fileName)
    [".fsx";".fsscript";".sketchfs"] |> List.exists ((=) ext)


let normalizePath (file : string) =
  if file.EndsWith ".fs" || file.EndsWith ".fsi" then
      let p = Path.GetFullPath file
      (p.Chars 0).ToString().ToLower() + p.Substring(1)
  else file

let inline combinePaths path1 (path2 : string) = Path.Combine(path1, path2.TrimStart [| '\\'; '/' |])

let inline (</>) path1 path2 = combinePaths path1 path2

let projectOptionsToParseOptions (checkOptions: FSharpProjectOptions) =
//TODO: Investigate why sometimes SourceFiles are not filled
  let files =
    match checkOptions.SourceFiles with
    | [||] -> checkOptions.OtherOptions |> Array.where (fun n -> n.EndsWith ".fs" || n.EndsWith ".fsx" || n.EndsWith ".fsi")
    | x -> x

  { FSharpParsingOptions.Default with SourceFiles = files}

[<RequireQualifiedAccess>]
module internal Option =

  let inline attempt (f: unit -> 'T) = try Some <| f() with _ -> None

[<RequireQualifiedAccess>]
module internal Async =
    /// Transforms an Async value using the specified function.
    [<CompiledName("Map")>]
    let map (mapping : 'a -> 'b) (value : Async<'a>) : Async<'b> =
        async {
            // Get the input value.
            let! x = value
            // Apply the mapping function and return the result.
            return mapping x
        }

    // Transforms an Async value using the specified Async function.
    [<CompiledName("Bind")>]
    let bind (binding : 'a -> Async<'b>) (value : Async<'a>) : Async<'b> =
        async {
            // Get the input value.
            let! x = value
            // Apply the binding function and return the result.
            return! binding x
        }

    let StartCatchCancellation(work, cancellationToken) =
        Async.FromContinuations(fun (cont, econt, _) ->
          // When the child is cancelled, report OperationCancelled
          // as an ordinary exception to "error continuation" rather
          // than using "cancellation continuation"
          let ccont e = econt e
          // Start the workflow using a provided cancellation token
          Async.StartWithContinuations( work, cont, econt, ccont, cancellationToken=cancellationToken) )

    [<RequireQualifiedAccess>]
    module Array =
        /// Async implementation of Array.map.
        let map (mapping : 'T -> Async<'U>) (array : 'T[]) : Async<'U[]> =
            let len = Array.length array
            let result = Array.zeroCreate len

            async { // Apply the mapping function to each array element.
                for i in 0 .. len - 1 do
                    let! mappedValue = mapping array.[i]
                    result.[i] <- mappedValue

                // Return the completed results.
                return result
}

// Maybe computation expression builder, copied from ExtCore library
/// https://github.com/jack-pappas/ExtCore/blob/master/ExtCore/Control.fs
[<Sealed>]
type internal MaybeBuilder () =
    // 'T -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Return value: 'T option =
        Some value

    // M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.ReturnFrom value: 'T option =
        value

    // unit -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Zero (): unit option =
        Some ()     // TODO: Should this be None?

    // (unit -> M<'T>) -> M<'T>
    [<DebuggerStepThrough>]
    member __.Delay (f: unit -> 'T option): 'T option =
        f ()

    // M<'T> -> M<'T> -> M<'T>
    // or
    // M<unit> -> M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member inline __.Combine (r1, r2: 'T option): 'T option =
        match r1 with
        | None ->
            None
        | Some () ->
            r2

    // M<'T> * ('T -> M<'U>) -> M<'U>
    [<DebuggerStepThrough>]
    member inline __.Bind (value, f: 'T -> 'U option): 'U option =
        Option.bind f value

    // 'T * ('T -> M<'U>) -> M<'U> when 'U :> IDisposable
    [<DebuggerStepThrough>]
    member __.Using (resource: ('T :> System.IDisposable), body: _ -> _ option): _ option =
        try body resource
        finally
            if not <| obj.ReferenceEquals (null, box resource) then
                resource.Dispose ()

    // (unit -> bool) * M<'T> -> M<'T>
    [<DebuggerStepThrough>]
    member x.While (guard, body: _ option): _ option =
        if guard () then
            // OPTIMIZE: This could be simplified so we don't need to make calls to Bind and While.
            x.Bind (body, (fun () -> x.While (guard, body)))
        else
            x.Zero ()

    // seq<'T> * ('T -> M<'U>) -> M<'U>
    // or
    // seq<'T> * ('T -> M<'U>) -> seq<M<'U>>
    [<DebuggerStepThrough>]
    member x.For (sequence: seq<_>, body: 'T -> unit option): _ option =
        // OPTIMIZE: This could be simplified so we don't need to make calls to Using, While, Delay.
        x.Using (sequence.GetEnumerator (), fun enum ->
            x.While (
                enum.MoveNext,
                x.Delay (fun () ->
                body enum.Current)))

[<Sealed>]
type internal AsyncMaybeBuilder () =
    [<DebuggerStepThrough>]
    member __.Return value : Async<'T option> = Some value |> async.Return

    [<DebuggerStepThrough>]
    member __.ReturnFrom value : Async<'T option> = value

    [<DebuggerStepThrough>]
    member __.ReturnFrom (value: 'T option) : Async<'T option> = async.Return value

    [<DebuggerStepThrough>]
    member __.Zero () : Async<unit option> =
        Some () |> async.Return

    [<DebuggerStepThrough>]
    member __.Delay (f : unit -> Async<'T option>) : Async<'T option> = f ()

    [<DebuggerStepThrough>]
    member __.Combine (r1, r2 : Async<'T option>) : Async<'T option> =
        async {
            let! r1' = r1
            match r1' with
            | None -> return None
            | Some () -> return! r2
        }

    [<DebuggerStepThrough>]
    member __.Bind (value: Async<'T option>, f : 'T -> Async<'U option>) : Async<'U option> =
        async {
            let! value' = value
            match value' with
            | None -> return None
            | Some result -> return! f result
        }

    [<DebuggerStepThrough>]
    member __.Bind (value: 'T option, f : 'T -> Async<'U option>) : Async<'U option> =
        async {
            match value with
            | None -> return None
            | Some result -> return! f result
        }

    [<DebuggerStepThrough>]
    member __.Using (resource : ('T :> IDisposable), body : _ -> Async<_ option>) : Async<_ option> =
        try body resource
        finally
            if not << isNull <| resource then resource.Dispose ()

    [<DebuggerStepThrough>]
    member x.While (guard, body : Async<_ option>) : Async<_ option> =
        if guard () then
            x.Bind (body, (fun () -> x.While (guard, body)))
        else
            x.Zero ()

    [<DebuggerStepThrough>]
    member x.For (sequence : seq<_>, body : 'T -> Async<unit option>) : Async<_ option> =
        x.Using (sequence.GetEnumerator (), fun enum ->
            x.While (enum.MoveNext, x.Delay (fun () -> body enum.Current)))

    [<DebuggerStepThrough>]
    member inline __.TryWith (computation : Async<'T option>, catchHandler : exn -> Async<'T option>) : Async<'T option> =
            async.TryWith (computation, catchHandler)

    [<DebuggerStepThrough>]
    member inline __.TryFinally (computation : Async<'T option>, compensation : unit -> unit) : Async<'T option> =
            async.TryFinally (computation, compensation)

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module internal AsyncMaybe =
    let inline liftAsync (async : Async<'T>) : Async<_ option> =
        async |> Async.map Some


[<RequireQualifiedAccess>]
[<CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module internal Array =
    let inline private checkNonNull argName arg =
        match box arg with
        | null -> nullArg argName
        | _ -> ()

    /// Optimized arrays equality. ~100x faster than `array1 = array2` on strings.
    /// ~2x faster for floats
    /// ~0.8x slower for ints
    let inline areEqual (xs: 'T []) (ys: 'T []) =
        match xs, ys with
        | null, null -> true
        | [||], [||] -> true
        | null, _ | _, null -> false
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
    let foldi (folder: 'State -> int -> 'T -> 'State) (state: 'State) (array: 'T []) =
        checkNonNull "array" array
        if array.Length = 0 then state else
        let folder = OptimizedClosures.FSharpFunc<_,_,_,_>.Adapt folder
        let mutable state:'State = state
        let len = array.Length
        for i = 0 to len - 1 do
            state <- folder.Invoke (state, i, array.[i])
        state

    /// Returns all heads of a given array.
    /// For [|1;2;3|] it returns [|[|1; 2; 3|]; [|1; 2|]; [|1|]|]
    let heads (array: 'T []) =
        checkNonNull "array" array
        let res = Array.zeroCreate<'T[]> array.Length
        for i = array.Length - 1 downto 0 do
            res.[i] <- array.[0..i]
        res

    /// check if subArray is found in the wholeArray starting
    /// at the provided index
    let inline isSubArray (subArray: 'T []) (wholeArray:'T []) index =
        if isNull subArray || isNull wholeArray then false
        elif subArray.Length = 0 then true
        elif subArray.Length > wholeArray.Length then false
        elif subArray.Length = wholeArray.Length then areEqual subArray wholeArray else
        let rec loop subidx idx =
            if subidx = subArray.Length then true
            elif subArray.[subidx] = wholeArray.[idx] then loop (subidx+1) (idx+1)
            else false
        loop 0 index

    /// Returns true if one array has another as its subset from index 0.
    let startsWith (prefix: _ []) (whole: _ []) =
        isSubArray prefix whole 0

    /// Returns true if one array has trailing elements equal to another's.
    let endsWith (suffix: _ []) (whole: _ []) =
        isSubArray suffix whole (whole.Length-suffix.Length)

    /// Returns a new array with an element replaced with a given value.
    let replace index value (array: _ []) =
        checkNonNull "array" array
        if index >= array.Length then raise (IndexOutOfRangeException "index")
        let res = Array.copy array
        res.[index] <- value
        res

    /// pass an array byref to reverse it in place
    let revInPlace (array: 'T []) =
        checkNonNull "array" array
        if areEqual array [||] then () else
        let arrlen, revlen = array.Length-1, array.Length/2 - 1
        for idx in 0 .. revlen do
            let t1 = array.[idx]
            let t2 = array.[arrlen-idx]
            array.[idx] <- t2
            array.[arrlen-idx] <- t1

    let splitAt (n : int) (xs : 'a[]) : 'a[] * 'a[] =
        match xs with
        | [||] | [|_|] -> xs, [||]
        | _ when n >= xs.Length || n < 0 -> xs, [||]
        | _ -> xs.[0..n-1], xs.[n..]

module internal List =

    ///Returns the greatest of all elements in the list that is less than the threshold
    let maxUnderThreshold nmax =
        List.maxBy(fun n -> if n > nmax then 0 else n)




[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module internal String =
    let inline toCharArray (str:string) = str.ToCharArray()

    let lowerCaseFirstChar (str: string) =
        if String.IsNullOrEmpty str
         || Char.IsLower(str, 0) then str else
        let strArr = toCharArray str
        match Array.tryHead strArr with
        | None -> str
        | Some c  ->
            strArr.[0] <- Char.ToLower c
            String (strArr)


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
               | index -> str.Substring (0, str.Length - index.Length), Some (int index)


    let (|StartsWith|_|) (pattern: string) (value: string) =
        if String.IsNullOrWhiteSpace value then
            None
        elif value.StartsWith pattern then
            Some()
        else None

    let split (splitter: char) (s: string) = s.Split([| splitter |], StringSplitOptions.RemoveEmptyEntries) |> List.ofArray

    let getLines (str: string) =
        use reader = new StringReader(str)
        [|
            let line = ref (reader.ReadLine())
            while not (isNull (!line)) do
                yield !line
                line := reader.ReadLine()
            if str.EndsWith("\n") then
                // last trailing space not returned
                // http://stackoverflow.com/questions/19365404/stringreader-omits-trailing-linebreak
                yield String.Empty
        |]

    type SplitResult =
    | NoMatch
    | Split of left: string * right: string

    let splitAtChar (splitter: char) (s: string) =
        match s.IndexOf splitter with
        | -1 -> NoMatch
        | n -> Split(s.[0..n-1], s.Substring(n+1))

type internal ConcurrentDictionary<'key, 'value> with
    member x.TryFind key =
        match x.TryGetValue key with
        | true, value -> Some value
        | _ -> None

type internal Path with
    static member GetFullPathSafe (path: string) =
        try Path.GetFullPath path
        with _ -> path

    static member GetFileNameSafe (path: string) =
        try Path.GetFileName path
        with _ -> path

    /// Algorithm from https://stackoverflow.com/a/35734486/433393 for converting file paths to uris,
    /// modified slightly to not rely on the System.Path members because they vary per-platform
    static member FilePathToUri (filePath: string): string =
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
                if (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ||
                    c = '+' || c = '/' || c = '.' || c = '-' || c = '_' || c = '~' ||
                    c > '\xFF' then
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
        else
            "untitled:" + filePath

    /// handles unifying the local-path logic for windows and non-windows paths,
    /// without doing a check based on what the current system's OS is.
    static member FileUriToLocalPath (uriString: string) =
        /// a test that checks if the start of the line is a windows-style drive string, for example
        /// /d:, /c:, /z:, etc.
        let isWindowsStyleDriveLetterMatch (s: string) =
            match s.[0..2].ToCharArray() with
            | [| |]
            | [| _ |]
            | [| _; _ |] -> false
            // 26 windows drive letters allowed, only
            | [| '/'; driveLetter; ':' |] when Char.IsLetter driveLetter -> true
            | _ -> false
        let initialLocalPath = Uri(uriString).LocalPath
        let fn =
            if isWindowsStyleDriveLetterMatch initialLocalPath
            then initialLocalPath.TrimStart('/')
            else initialLocalPath
        if uriString.StartsWith "untitled:" then (fn + ".fsx") else fn

let inline debug msg = Printf.kprintf Debug.WriteLine msg
let inline fail msg = Printf.kprintf Debug.Fail msg
let asyncMaybe = AsyncMaybeBuilder()
let maybe = MaybeBuilder()


let chooseByPrefix (prefix: string) (s: string) =
    if s.StartsWith(prefix) then Some (s.Substring(prefix.Length))
    else None

let chooseByPrefix2 prefixes (s: string) =
    prefixes
    |> List.tryPick (fun prefix -> chooseByPrefix prefix s)

let splitByPrefix (prefix: string) (s: string) =
    if s.StartsWith(prefix) then Some (prefix, s.Substring(prefix.Length))
    else None

let splitByPrefix2 prefixes (s: string) =
    prefixes
    |> List.tryPick (fun prefix -> splitByPrefix prefix s)

[<AutoOpen>]
module internal Patterns =

    let (|StartsWith|_|) (pat : string) (str : string)  =
        match str with
        | null -> None
        | _ when str.StartsWith pat -> Some str
        | _ -> None

    let (|Contains|_|) (pat : string) (str : string)  =
        match str with
        | null -> None
        | _ when str.Contains pat -> Some str
        | _ -> None


module internal Version =

  open System.Reflection

  type VersionInfo = { Version: string; GitSha: string }

  let private informationalVersion () =
    let assemblies = typeof<VersionInfo>.Assembly.GetCustomAttributes(typeof<AssemblyInformationalVersionAttribute>, true)
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

    { VersionInfo.Version = version; GitSha = sha }

//source: https://nbevans.wordpress.com/2014/08/09/a-simple-stereotypical-javascript-like-debounce-service-for-f/
type internal Debounce<'a>(timeout, fn) =
    let debounce fn timeout = MailboxProcessor<'a>.Start(fun agent ->
        let rec loop ida idb arg = async {
            let! r = agent.TryReceive(timeout)
            match r with
            | Some arg -> return! loop ida (idb + 1) (Some arg)
            | None when ida <> idb -> fn arg.Value; return! loop idb idb None
            | None -> return! loop ida idb arg
        }
        loop 0 0 None)

    let mailbox = debounce fn timeout

    /// Calls the function, after debouncing has been applied.
    member __.Bounce(arg) = mailbox.Post(arg)

/// OS-local, normalized path
type [<Measure>] internal LocalPath
/// An HTTP url
type [<Measure>] internal Url
/// OS-Sensitive path segment from some repository root
type [<Measure>] internal RepoPathSegment
// OS-agnostic path segment from some repository root
type [<Measure>] internal NormalizedRepoPathSegment
