# Magic Commands

A magic command is a scriptable shortcut to a more complex behavior. The magic command concept is familiar to Jupyter users. With a slight change in syntax to accommodate the .NET languages, they're also available in .NET Interactive. 

Magic commands must always occur at the beginning of a line and are prefixed with either `#!` or, less commonly, `#`. The latter occurs only when unifying behaviors with language-specific compiler directives such as `#r`, a compiler directive that's implemented in both C# and F# script. Another difference from Jupyter's magic commands is that in .NET Interactive there is no distinction between a "cell magic" and a "line magic". 

Here's an example using the `#!time` magic command:

<img src="https://user-images.githubusercontent.com/547415/81481309-ec858b00-91e3-11ea-9f80-36f02ab64e32.png" />

Magic commands use a command line-style syntax, including options and arguments similar to a command line tool. For every magic command, you can get help using `-h`:

![image](https://user-images.githubusercontent.com/547415/81481559-f3ad9880-91e5-11ea-909a-f969525bda8d.png)

The following is a list of magic commands supported by .NET Interactive:

## .NET Kernel

The following magic commands are available globally.


| Command                                 | Behavior                               
|-----------------------------------------|----------------------------------------------------------------------
| `#!csharp` (also: `#!c#`, `#!C#`)       | Indicates that the code that follows is C#. Specifically, it is the [C# scripting](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting) dialect. 
| `#!fsharp` (also: `#!f#`, `#!F#`)       | Indicates that the code that follows is F#.
| `#!pwsh` (also: `#!powershell`)         | Indicates that the code that follows is PowerShell.
| `#!javascript` (also: `#!js`)           | Indicates that the code that follows is JavaScript, to be executed in the browser.
| `#!html`                                | Indicates that the code that follows is HTML, which can then be directly rendered in the browser.
| `#!lsmagic`                             | Lists the available magic commands, including those that might have been installed via an extension. 
| `#!markdown`                            | Indicates that the code that follows is Markdown, which can then be directly rendered as HTML in the browser.
| `#!log`                                 | Enables logging for the session. Once it has been run, detailed log information from .NET Interactive will be published along with other code outputs. 
| `#!about`                               | Displays information about the current version of .NET Interactive:<br />![image](https://user-images.githubusercontent.com/547415/81481060-42f1ca00-91e2-11ea-92f7-c4ffae904961.png)
| `#!time`                                | Measures the execution time of the code submission.
| `#!value`                               | Stores a value (from entered text, a file, or a URL), which can be accessed using `#!share`.

## C# Kernel

The following magic commands are available within a C# language context.

| Command                                 | Behavior                               
|-----------------------------------------|----------------------------------------------------------------------
| `#i`                                    | Adds a NuGet source to the session. Subsequent `#r nuget` commands will include the specified source when resolving packages.
| `#r`                                    | In C# Interactive, the `#r` directive adds a reference to a specified assembly, e.g. `#r "/path/to/a.dll"`.  In .NET Interactive, this capability has been expanded to provide the ability to reference NuGet packages.<br />![image](https://user-images.githubusercontent.com/547415/81502691-362dae80-9294-11ea-94a4-266f4edc0d5e.png)<br />You cannot reference two different versions of the same package. If you try to do so, you'll receive an error:<br />![image](https://user-images.githubusercontent.com/547415/81502694-3cbc2600-9294-11ea-92d4-9151ad1bc805.png)
| `#!share`                               | Shares a variable from another specified subkernel (including one stored using `#!value`).
| `#!who`                                 | Displays the names of the top-level variables within the C# subkernel.
| `#!whos`                                | Displays the top-level variables within the C# subkernel, including their name, type, and value.<br />![image](https://user-images.githubusercontent.com/547415/81481511-87329980-91e5-11ea-9a4b-b025435553ff.png)

## F# Kernel

The following magic commands are available within an F# language context.

| Command                                 | Behavior                               
|-----------------------------------------|----------------------------------------------------------------------
| `#i`                                    | Adds a NuGet source to the session. Subsequent `#r nuget` commands will include the specified source when resolving packages.
| `#r`                                    | In F# Interactive, the `#r` directive adds a reference to a specified assembly, e.g. `#r "/path/to/a.dll"`.  In F# 5, which is used by .NET Interactive, this capability has been expanded to provide the ability to reference NuGet packages.<br />![image](https://user-images.githubusercontent.com/547415/81502691-362dae80-9294-11ea-94a4-266f4edc0d5e.png)<br />You cannot reference two different versions of the same package. If you try to do so, you'll receive an error:<br />![image](https://user-images.githubusercontent.com/547415/81502694-3cbc2600-9294-11ea-92d4-9151ad1bc805.png)
| `#!share`                               | Shares a variable from another specified subkernel (including one stored using `#!value`).
| `#!who`                                 | Displays the names of the top-level variables within the F# subkernel.
| `#!whos`                                | Displays the top-level variables within the F# subkernel, including their name, type, and value.<br />![image](https://user-images.githubusercontent.com/547415/81481474-636f5380-91e5-11ea-92ce-07336b201db0.png)

## Extensibility

Magic commands can be added dynamically to .NET Interactive's kernels, either at the global level or to individual language-specific subkernels. You can learn more about how to do this [here](extending-dotnet-interactive.md#adding-magic-commands).