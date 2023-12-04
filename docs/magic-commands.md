# Magic Commands

A magic command is a special code command that can be run in an interactive code submission. The magic command concept is familiar to Jupyter users. With a slight change in syntax to accommodate the .NET languages, they're also available in .NET Interactive.

Magic commands must always start at the beginning of a line, cannot span more than one line, and are prefixed with either `#!` or, less commonly, `#`. The latter occurs only when unifying behaviors with language-specific compiler directives such as `#r`, a compiler directive that's implemented in both C# and F# script. Unlike Jupyter's magic commands, .NET Interactive there is no distinction between a "cell magic" and a "line magic". 

Here's an example using the `#!time` magic command:

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/213319133-9c03bbad-09f6-43a9-9981-1a82826f06aa.png">

Magic commands use a command line-style syntax, including options and arguments similar to a command line tool. For every magic command, you can get help using `-h`:

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/213322348-f9754fd3-aa39-49bc-9f97-d7d765f9177c.png">

The following is a list of magic commands supported by .NET Interactive:

## Global magic commands

The following are some useful magic commands that are available in all or nearly all subkernels within the .NET Interactive kernel:

| Command                               | Behavior                               
|---------------------------------------|----------------------------------------------------------------------
| `#!about`                             | Displays information about the version of the kernel.
| [`#!import`](import-magic-command.md) | Runs code from another notebook or source code file.
| `#!lsmagic`                           | Lists the available magic commands (including those that might have been installed via an extension). 
| `#!markdown`                          | Indicates that the code that follows is Markdown, which can then be directly rendered as HTML in the browser.
| [`#!share`](variable-sharing.md)      | Shares a variable from another specified subkernel (including one stored using `#!value`).
| [`#!set`](variable-sharing.md)        | Sets a value in the current kernel (including support for inputs and literals).
| `#!time`                              | Measures the execution time of the code submission.
| `#!connect`                           | Enables connection of additional kernels.

## Kernel chooser magic commands

Each subkernel in .NET Interactive, including subkernels added dynamically at runtime, has a name which is unique within the kernel. This name can be used to send code to that subkernel. In the default .NET Interactive kernel, the following kernel chooser magic commands are available. 

| Command                               | Behavior                               
|---------------------------------------|----------------------------------------------------------------------
| `#!csharp` (also: `#!c#`, `#!C#`)     | Indicates that the code that follows is C#. (Specifically, the [C# Script](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting) dialect.) 
| `#!fsharp` (also: `#!f#`, `#!F#`)     | Indicates that the code that follows is F#.
| `#!html`                              | Indicates that the code that follows is HTML, which can then be directly rendered in the browser.
| `#!javascript` (also: `#!js`)         | Indicates that the code that follows is JavaScript, to be executed in the browser.
| `#!kql`                               | Provides information on how to add connection-specific KQL (Kusto Query Language) kernels to your interactive session.
| `#!mermaid`                           | Indicates that the code that follows is [Mermaid](https://mermaid.js.org/intro/). Output is rendered visually.
| `#!pwsh` (also: `#!powershell`)       | Indicates that the code that follows is PowerShell.
| `#!sql`                               | Provides information on how to add connection-specific SQL kernels to your interactive session.
| `#!value`                             | Stores a value (from entered text, a file, or a URL), which can be accessed using `#!share`.

Note that in the Polyglot Notebooks extension for VS Code, you can also click on the kernel name in the lower right-hand corner of each cell to choose a kernel, so it's not necessary to use these magics. But in Jupyter and other frontends that don't have UI to allow you to choose a kernel, these magics give you access to the same capabilities.

## C# Kernel

The following magic commands are available within a C# language context.

| Command                                 | Behavior                               
|-----------------------------------------|----------------------------------------------------------------------
| `#i`                                    | Adds a NuGet source to the session. Subsequent `#r nuget` commands will include the specified source when resolving packages.
| `#r`                                    | In C# Interactive, the `#r` compiler directive adds a reference to a specified assembly, e.g. `#r "/path/to/a.dll"`, or system assembly, e.g. `#r "System.Net.Http.Json.dll"`.  In .NET Interactive, this capability has been expanded to provide the ability to reference NuGet packages using the syntax `#r "nuget:PackageName,1.0.0-beta"`.<br />You cannot reference two different versions of the same package. If you try to do so, you'll receive an error.
| `#!who`                                 | Displays the names of the top-level variables within the C# subkernel.
| `#!whos`                                | Displays the top-level variables within the C# subkernel (including their name, type, and value).

## F# Kernel

The following magic commands are available within an F# language context.

| Command                                 | Behavior                               
|-----------------------------------------|----------------------------------------------------------------------
| `#i`                                    | Adds a NuGet source to the session. Subsequent `#r nuget` commands will include the specified source when resolving packages.
| `#r`                                    | In F# Interactive, the `#r` compiler directive adds a reference to a specified assembly, e.g. `#r "/path/to/a.dll"`. <br/> When used with the `nuget` qualifier, it can also reference NuGet packages using the syntax `#r "nuget:PackageName,1.0.0-beta"`.<br />You cannot reference two different versions of the same package. If you try to do so, you'll receive an error.
| `#!who`                                 | Displays the names of the top-level variables within the F# subkernel.
| `#!whos`                                | Displays the top-level variables within the F# subkernel (including their name, type, and value).

## Extensibility

Magic commands can be added dynamically to .NET Interactive's kernels, either at the global level or to individual language-specific subkernels. You can learn more about how to do this [here](extending-dotnet-interactive.md#adding-magic-commands).
