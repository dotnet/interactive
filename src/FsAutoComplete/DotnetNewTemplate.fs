namespace FsAutoComplete
//open System

//module DotnetNewTemplate =
//  type Template = {
//    Name : string;
//    ShortName : string;
//    Language: TemplateLanguage list;
//    Tags: string list
//  }
//  and TemplateLanguage = CSharp | FSharp | VB
//  and DetailedTemplate = {
//    TemplateName : string;
//    Author : string;
//    TemplateDescription : string;
//    Options : TemplateParameter list;
//  }
//  and TemplateParameter = {
//    ParameterName : string;
//    ShortName : string;
//    ParameterType : TemplateParameterType;
//    ParameterDescription : string;
//    DefaultValue : string
//  }
//  and TemplateParameterType =
//  | Bool
//  | String
//  | Choice of string list

//  let installedTemplates () : Template list =
//    let readTemplates () =

//      let si = System.Diagnostics.ProcessStartInfo()
//      si.FileName <- "dotnet"
//      si.Arguments <- "new --list -lang F#"
//      si.UseShellExecute <- false
//      si.RedirectStandardOutput <- true
//      si.WorkingDirectory <- Environment.CurrentDirectory
//      let proc = System.Diagnostics.Process.Start(si)
//      let mutable output = ""
//      while not proc.StandardOutput.EndOfStream do
//          let line = proc.StandardOutput.ReadLine()
//          output <- output + "\n" + line
//      output

//    let parseTemplateOutput (x: string) =
//        let xs =
//            x.Split('\n')
//            |> Array.skipWhile(fun n -> not (n.StartsWith "Templates"))
//            |> Array.filter (fun n -> not (String.IsNullOrWhiteSpace n))
//        let header = xs.[0]
//        let body = xs.[2..]
//        let nameLegth = header.IndexOf("Short")
//        printfn "Length: %A" nameLegth
//        let body =
//            body
//            |> Array.map (fun (n: string) ->
//                printfn "ROW: %s" n
//                let name = n.[0..nameLegth - 1].Trim()
//                let shortName = n.[nameLegth..].Split(' ').[0].Trim()
//                name, shortName
//            )

//        body

//    readTemplates ()
//    |> parseTemplateOutput
//    |> Array.map (fun (name, shortName) ->
//      {Name = name; ShortName = shortName; Language = []; Tags = []}
//    )
//    |> Array.toList

//  let templateDetails () : DetailedTemplate list =
//    [
//      { TemplateName = "Console Application";
//        Author = "Microsoft";
//        TemplateDescription = "A project for creating a command-line application that can run on .NET Core on Windows, Linux and macOS";
//        Options =
//        [ { ParameterName = "--no-restore";
//            ShortName = "";
//            ParameterType = TemplateParameterType.Bool;
//            ParameterDescription = "If specified, skips the automatic restore of the project on create.";
//            DefaultValue = "false / (*) true" };
//        ] };

//      { TemplateName = "Class library";
//        Author = "Microsoft";
//        TemplateDescription = "A project for creating a class library that targets .NET Standard or .NET Core";
//        Options =
//        [ { ParameterName = "--framework";
//            ShortName = "-f";
//            ParameterType = TemplateParameterType.Choice ["netcoreapp2.1     - Target netcoreapp2.1";"netstandard2.0    - Target netstandard2.0"];
//            ParameterDescription = "The target framework for the project.";
//            DefaultValue = "netstandard2.0" };

//          { ParameterName = "--no-restore";
//            ShortName = "";
//            ParameterType = TemplateParameterType.Bool;
//            ParameterDescription = "If specified, skips the automatic restore of the project on create.";
//            DefaultValue = "false / (*) true" };

//        ] };
//    ]

//  let isMatch (filterstr: string) (x: string) =
//    x.ToLower().Contains(filterstr.ToLower())

//  let nameMatch (filterstr: string) (x: string) =
//    x.ToLower() = filterstr.ToLower()

//  let extractString (t : Template) =
//    [t.Name; t.ShortName]

//  let extractDetailedString (t : DetailedTemplate) =
//    [t.TemplateName]

//  let convertObjToString (o: obj) : string =
//    let result =
//      match o with
//      | :? string as s -> sprintf "%s" s
//      | :? bool as s -> if s then "true" else "false"
//      | :? (string list) as str -> String.concat ", " (str|> List.map string)
//      | _ -> failwithf "The value %A is not supported as parameter" o
//    result


//  let dotnetnewgetDetails (userInput : string) =
//    let templates =
//      templateDetails ()
//      |> List.map (fun t -> t, extractDetailedString t)
//      |> List.filter (fun (t,strings) -> strings |> List.exists (nameMatch userInput))
//      |> List.map (fun (t,strings) -> t)

//    match templates with
//    | [] -> failwithf "No template exists with name : %s" userInput
//    | [x] -> x
//    | _ -> failwithf "Multiple templates found : \n%A" templates

//  let dotnetnewCreateCli (templateShortName : string) (name: string option) (output: string option) (parameterList : (string * obj) list) =
//    let str = "new " + templateShortName + " -lang F#"
//    let str =
//      match name with
//      | None -> str
//      | Some s -> str + " -n " + s
//    let str =
//      match output with
//      | None -> str
//      | Some s -> str + " -o " + s

//    let plist =
//      parameterList
//      |> List.map ( fun (k,v) ->
//               let asString = convertObjToString v
//               k,asString )

//    let str2 =
//      plist
//      |> List.map ( fun(k,v) ->
//                let theString = k + " " + v
//                theString )
//      |> String.concat " "

//    let args = str + " " + str2

//    let si = Diagnostics.ProcessStartInfo()
//    si.FileName <- "dotnet"
//    si.Arguments <- args
//    si.UseShellExecute <- false
//    si.RedirectStandardOutput <- true
//    si.WorkingDirectory <- Environment.CurrentDirectory
//    let proc = Diagnostics.Process.Start(si)
//    Utils.ProcessHelper.WaitForExitAsync proc
