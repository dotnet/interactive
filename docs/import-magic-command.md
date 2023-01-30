# The `#!import` magic command

Often when working in a notebook, there's code in another file that you'd like to be able to use without copying it into the notebook. Notebook and scripting tools typically include ways to run code from an external file, and .NET Interactive is no different. The `#!import` magic command provides this capability. When run, the `#!import` command will read the file from the specified path and immediately execute the code found within it.

```console
#!import /path/to/file
```

For imported notebook files containing multiple cells, the cells will be run in the order in which they occur in the file.

## `#!import` and polyglot

Since .NET Interactive is polyglot, the `#!import` magic command recognizes multiple languages. If the specified file is in a format that supports polyglot, such as `.ipynb` or `.dib`, then the different code snippets within the file will be run using their appropriate subkernels, as long as the importing kernel has subkernels that correspond to those languages.

But `#!import` isn't limited to importing other notebook files. It can also directly load source code files for known languages, including:

|File extension | Details                                                                 |
|---------------|-------------------------------------------------------------------------|
| `.cs`         | Common C# <br> _(Note: These will be compiled using the C# Script compiler.)_
| `.csx`        | C# Script
| `.fs`         | Common F#
| `.fsx`        | F# script
| `.html`       | HTML
| `.js`         | JavaScript
| `.ps1`        | PowerShell 

Note that `.cs` and `.fs` files will be run using .NET Interactive's default scripting implementations, so some language constructs might not be supported. 

## .NET Projects

Importing or referencing full .NET projects, or source files in the context of a .NET project, is not currently supported. If you have functionality in a .NET project that you would like to use from within a notebook, you can compile it and then reference the assembly directly using `#r "/path/to/assembly.dll"`, or package it and reference the NuGet package using [`#r nuget`](nuget-overview.md).

Improved integration with .NET projects is being considered, however. For more information, see issue [#890](https://github.com/dotnet/interactive/issues/890).
