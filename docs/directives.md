# Directives, or "magic commands"

A directive is a scriptable shortcut to a more complex behavior. Directives are similar to the magic command concept familiar to IPython and Jupyter users. There is no distinction in .NET Interactive between a "cell magic" and a "line magic". 

Directives must always occur at the beginning of a line and are prefixed with either `#!` or, less commonly, `#`. (The latter occurs only when unifying behaviors with language-specific compiler directives such as `#r`, implemented in both C# and F# script.)

Here's an example using the `#!time` directive:

<img src="https://user-images.githubusercontent.com/547415/81481309-ec858b00-91e3-11ea-9f80-36f02ab64e32.png" />

Directives use a command-line syntax, including options and arguments similar to a command line tool. For every directive, you can get help using `-h`:

![image](https://user-images.githubusercontent.com/547415/81481559-f3ad9880-91e5-11ea-909a-f969525bda8d.png)

The following is a list of directives supported by .NET Interactive:

## .NET Kernel

The following directives are available globally.

### `#!csharp` (also: `#!c#`, `#!C#`)

This directive indicates that the code that follows is C#. Specifically, it is the [C# scripting](https://docs.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting) dialect. 

### `#!fsharp` (also: `#!f#`, `#!F#`)

This directive indicates that the code that follows is F#.

### `#!pwsh` (also: `#!powershell`)

This directive indicates that the code that follows is PowerShell.

### `#!javascript` (also: `#!js`)

This directive indicates that the code that follows is JavaScript, to be executed in the browser.

### `#!html`

This directive indicates that the code that follows is HTML, which can then be directly rendered in the browser.

### `#!lsmagic`

Lists the available magic commands / directives, including those that might have been installed via an extension. 

### `#!markdown`

This directive indicates that the code that follows is Markdown, which can then be directly rendered as HTML in the browser.

### `#!time`

This directive measures the execution time of the code submission.

### `#!log`

The `#!log` command enables session logging for the session. Once it has been run, detailed log information from .NET Interactive will be published along with other code outputs. 

### `#!about`

The `#!about` directive displays information about the current version of .NET Interactive:

<img src="https://user-images.githubusercontent.com/547415/81481060-42f1ca00-91e2-11ea-92f7-c4ffae904961.png" />



## C# Kernel

The following directives are available within a C# language context.

### `#i`

The `#i` directive adds a NuGet source to the session. Subsequent `#r nuget` commands will include the specified source when resolving packages.

### `#r`

In C# Interactive, the `#r` directive adds a reference to a specified assembly, e.g. `#r "/path/to/a.dll"`.  In .NET Interactive, this capability has been expanded to provide the ability to reference NuGet packages.

![image](https://user-images.githubusercontent.com/547415/81502691-362dae80-9294-11ea-94a4-266f4edc0d5e.png)

You cannot reference two different versions of the same package. If you try to do so, you'll receive an error:

![image](https://user-images.githubusercontent.com/547415/81502694-3cbc2600-9294-11ea-92d4-9151ad1bc805.png)

### `#!who` 

The `#!who` command displays the names of the top-level variables within the C# subkernel.

### `#!whos`

The `#!whos` command displays the top-level variables within the C# subkernel, including their name, type, and value.

![image](https://user-images.githubusercontent.com/547415/81481511-87329980-91e5-11ea-9a4b-b025435553ff.png)

## F# Kernel

The following directives are available within an F# language context.

### `#i`

The `#i` directive adds a NuGet source to the session. Subsequent `#r nuget` commands will include the specified source when resolving packages.

### `#r`

In F# Interactive, the `#r` directive adds a reference to a specified assembly, e.g. `#r "/path/to/a.dll"`.  In F# 5, which is used by .NET Interactive, this capability has been expanded to provide the ability to reference NuGet packages.

![image](https://user-images.githubusercontent.com/547415/81502691-362dae80-9294-11ea-94a4-266f4edc0d5e.png)

You cannot reference two different versions of the same package. If you try to do so, you'll receive an error:

![image](https://user-images.githubusercontent.com/547415/81502694-3cbc2600-9294-11ea-92d4-9151ad1bc805.png)

### `#!who` 

The `#!who` command displays the names of the top-level variables within the F# subkernel.



### `#!whos`

The `#!whos` command displays the top-level variables within the F# subkernel, including their name, type, and value.

![image](https://user-images.githubusercontent.com/547415/81481474-636f5380-91e5-11ea-92ce-07336b201db0.png)

## Extensibility

Directives can be added dynamically to .NET Interactive's kernels, either at the global level or to individual language-specific subkernels.