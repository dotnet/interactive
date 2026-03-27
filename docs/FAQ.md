# FAQ

* [Definitions and concepts](#definitions-and-concepts)
* [Using Polyglot Notebooks](#using-polyglot-notebooks)
* [Troubleshooting](#troubleshooting)

## _Definitions and concepts_

### What is a notebook?

A "computational notebook" is a type of program that allows mixing formatted text and executable code to create documents with runnable examples. Notebooks are an example of [literate programming](https://en.wikipedia.org/wiki/Literate_programming). A notebook has "cells" which are different text regions. There are commonly three different kinds of cells: 

* _Code cells_ contain runnable code.
* _Output cells_ contain the result from the last execution of the associated code cell. 
* _Markdown cells_ are display-only and can be used to edit and display richly-formatted (but static) text, including hyperlinks, images, diagrams, and so on. 

Project Jupyter, which is probably the most well-known notebook technology, has grown from its origins in scientific and academic environments to become a mainstay in industry for data analysis and data science. Other use cases include interactive documentation and learning materials, troubleshooting guides, and self-guiding automation scripts that can capture structured or visual log output.

### What is Jupyter Notebook?

Jupyter Notebook is a browser-based notebook UI (or "frontend") from Project Jupyter that can edit and run notebooks using a Jupyter kernel and the `.ipynb` file format.

### What is JupyterLab?

JupyterLab is another browser-based frontend for Jupyter kernels, similar to Jupyter Notebook, but richer and more extensible.

### What is a frontend?

The term "frontend" is often used to describe the UI for a notebook editor. Jupyter Notebook, JupyterLab, nteract Desktop, and the [Jupyter Extension](https://marketplace.visualstudio.com/items?itemName=ms-toolsai.jupyter) for VS Code and GitHub Codespaces are examples of notebook frontends.

### What is a kernel?

A kernel is an execution engine for a notebook. While the notebook UI (or "frontend") is responsible for displaying text, code, and execution results, the kernel is where the actual processing of the code takes place. Kernels are UI-agnostic. They typically run in a different process from the UI, and can often run on a different machine.

In .NET Interactive, there might be several kernels within a single kernel process. We refer to these kernels as "subkernels." Each one represents a stateful unit of computation with a set of capabilities that can include running code, sharing variables, and providing language services (e.g. code completions, diagnostic squiggles, and inline help) for a specific language. A kernel in .NET Interactive does not need to be in its own process. Multiple .NET Interactive subkernels can work together in a single notebook session whether they share a process or are distributed across multiple machines.

### What is a Jupyter kernel?

A Jupyter kernel is any kernel that implements the [Jupyter Message Protocol (JMP)](https://jupyter-client.readthedocs.io/en/stable/messaging.html#). The most commonly-used Jupyter kernel is [IPython](https://en.wikipedia.org/wiki/IPython), an interactive shell for Python, from which Project Jupyter grew. 

.NET Interactive is a Jupyter kernel when started in Jupyter mode (using the command line `dotnet interactive jupyter`).

### What is .NET Interactive?

.NET Interactive is a [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) ([`dotnet-interactive`](https://www.nuget.org/packages/Microsoft.dotnet-interactive)) containing a general-purpose engine for interactive programming including, but not limited to, use with notebooks. .NET Interactive has a progammatic interface but no graphical user interface. Various notebook frontends can be used to provide a GUI over .NET Interactive. It includes suport for a number of languages, including C#, F#, PowerShell, and JavaScript, as well as the ability to load support for additional languages such as SQL and Python.

### What is the difference between .NET Interactive and Jupyter?

The .NET Interactive kernel (i.e. the `dotnet-interactive` [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools)) is a Jupyter-compliant kernel that can be used with Project Jupyter just like any other kernel. Like other Jupyter kernels, .NET Interactive isn't opinionated about which frontend you use. 

The Polyglot Notebooks extension for VS Code isn't required to use the .NET Interactive kernel, but it does provide access to some additional functionality that isn't typical of Jupyter frontends, such as the ability to switch languages (i.e. subkernels) on a per-cell basis.

### What is Polyglot Notebooks?

[Polyglot Notebooks](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode) is an extension for Visual Studio Code that provides a notebook frontend and related tools for editing and running notebooks with the .NET Interactive kernel.

### What is the difference between Polyglot Notebooks and .NET Interactive?

[Polyglot Notebooks](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode) is a notebook frontend. It allows you read and write notebook files, run the code in the notebook, and visualize the results.

[.NET Interactive](https://github.com/dotnet/interactive) is a kernel. It has an API but no user interface. When you press a cell's run button in a frontend such as Polyglot Notebooks, it sends a message to the kernel. The kernel processes the response and sends messages  back, including code execution results for the frontend to display.

### What is a subkernel?

A subkernel is a concept in .NET Interactive that describes one among many kernels within a single notebook session. Each languages available in a Polyglot Notebook corresponds to a different subkernel. Subkernels can also be added dynamically at runtime, for example when connecting to a data source or a kernel running in a remote process.

### What is a proxy kernel?

A proxy kernel is a concept in .NET Interactive that describes a subkernel that proxies a remote kernel. A proxy kernel can be used locally just like any other subkernel. Proxy kernels allow you to create notebooks that combine kernels running in multiple different processes or on different machines. 

One prominent example of a proxy kernel is the JavaScript kernel. The actual implementation is written in TypeScript and runs in a separate process from the .NET Interactive kernel. For example, in the Polyglot Notebooks extension, the JavaScript kernel runs within the same web view that renders the notebook's HTML output. But this kernel can be called programmatically in .NET using the same APIs used to call in-process kernels such as the C# kernel. The proxy kernel serves as the adapter that enables this.

```csharp
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

var javascriptCode = "console.log('hello from JavaScript!')";

await Kernel.Root.SendAsync(
    new SubmitCode(
        javascriptCode, 
        "javascript"));
```

### What's the difference between a `.dib` file and an `.ipynb` file?

The `.ipynb` file extension is the standard Jupyter notebook format. Despite the name, it's no longer specific to IPython, and can be used for many different languages. It's a JSON-based format and it can store content and metadata for code cells, Markdown cells, and cell outputs, which store the results of code execution for display. Multiple outputs can be stored for each code cell, as long as they differ by MIME type. There are many tools available for diffing, converting, and displaying `.ipynb` files. In GitHub,`.ipynb` files are displayed using a notebook-style layout.

The [Polyglot Notebooks extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.dotnet-interactive-vscode) can read and write the `.ipynb` format. It can also read and write a different format, `.dib`. Unlike `.ipynb`, which is a presentation-focused document format, `.dib` is a scripting format. It does not store outputs, and the raw code of a `.dib` can be pasted into a single Polyglot Notebook cell and run directly. This format can contain multiple languages delimited by kernel selector magic commands (e.g. `#!csharp`). The `.dib` format is also a plain text format, not JSON. It's easier to diff without the need for special tools and the contents don't need any special escaping.

### What is a magic command?

A magic command is a special code command that can be run in a notebook cell, typically using a different syntax than the primary language supported by the kernel.

In IPython, magic commands are prefixed with `%`. Since this is not a valid operator in common Python, it allows IPython to easily identify magic commands.

In .NET Interactive, magics are instead prefixed with `#!`, since the `#` character indicates a comment or preprocessor directive in all of the major .NET languages. Magics must come at the beginning of the line and, while multiple magics can be used in a single cell, a single magic cannot span more than one line. In .NET Interactive, there is no distinction between a "cell magic" and a "line magic". 

### What is a REPL?

A "read-eval-print loop", or [REPL](https://en.wikipedia.org/wiki/Read%E2%80%93eval%E2%80%93print_loop), is an interactive text-based interface for incrementally creating a program, providing it with input, and seeing its output. While historically one usually interacts with a REPL through a terminal, there are also a number of GUI-based REPLs. Notebooks can be considered an example. 

### What is .NET REPL?

.NET Repl is a [.NET tool](https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools) ([`dotnet-repl`](https://github.com/jonsequitur/dotnet-repl)) that uses the .NET Interactive engine to provide a terminal-based REPL supporting the same general features that you can find in Polyglot Notebooks, including support for combining multiple languages in one session and support for the `.ipynb` and `.dib` file formats. It also provides some additional features, including the ability to execute notebooks without a UI, allowing for testing notebook files or using them as automation scripts with built-in log capture.

[_This is an experimental feature that might be added to the core .NET Interactive product in the future._]

### What is the C# Interactive Window?

The C# Interactive Window is a REPL window in Visual Studio that lets you write and execute code using the C# Script dialect.

### What is C# Script (a.k.a. Roslyn Scripting)?

[C# Script](https://learn.microsoft.com/en-us/archive/msdn-magazine/2016/january/essential-net-csharp-scripting) is a dialect of the C# language that can be used for interactive programming, as well as scripting using the `.csx` file format. It differs from common C# in a few key ways, such as its lack of support for namespaces. The library that provides the core Roslyn Scripting functionality, [Microsoft.CodeAnalysis.CSharp.Scripting](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting), provides .NET Interactive's default C# functionality.

### What is F# scripting?

The F# language can be used for scripting but does not have a separate dialect like C# does. The `.fsx` file format is used for scripting in F#.

### What is the F# Interactive Window?

The F# Interactive Window is a REPL window in Visual Studio that lets you write and execute code using the F# language.

### What is CSI?

CSI stands for C# Interactive and refers to a command-line C# REPL, `csi.exe`, powered by Roslyn Scripting. It is not directly used by .NET Interactive.

### What is FSI?

FSI stands for F# Interactive and refers to a command line F# REPL, `fsi.exe` (also invokable via `dotnet fsi`). This tool can be used as an interactive command-ine REPL or to run F# scripts (`.fsx` files).

### How is .NET Interactive related to Try .NET?

Try .NET is a web application that runs snippets of C# code and shows the result as text output. The code is compiled on the Try .NET server and then executed in the browser using Blazor WebAssembly. It differs from .NET Interactive in that it uses common C#, not C# Script.

The upcoming version of Try .NET reimplements the core Try .NET functionality as a .NET Interactive kernel, bringing common C# to notebooks. This is still a work in progress.

## _Using Polyglot Notebooks_

### How do I see what variables have been declared?

While in a Polyglot Notebook, open the VS Code command palette and select `Polyglot Notebook: Focus on Variables View`. This will open the Panel to the `POLYGLOT NOTEBOOK: VARIABLES` tab, which displays the variables in all of the different loaded kernels.

### Can I access the contents of an output cell programmatically?

From within a running notebook, it's currently not possible to directly access the outputs. The data used to populate them is transient and the frontend, not the kernel, is responsible for writing the file. They outputs are stored in the `.ipynb` file though and can be read from there if you have the path to the file. The .NET Interactive packages include a library for parsing and writing various file formats including `.ipynb` and `.dib`: [Microsoft.DotNet.Interactive.Documents](https://www.nuget.org/packages/Microsoft.DotNet.Interactive.Documents). 

### Can I use Polyglot Notebooks in GitHub CodeSpaces?

Yes! In Codespaces, the Polyglot Notebooks extension can be loaded from the VS Code Marketplace.

### Can I use Polyglot Notebooks in github.dev? 

While the Polyglot Notebooks extension can be loaded in github.dev, it can only be used to view notebooks. Code execution is not available currently in the browser-only environment.

### Can I use .NET Interactive with Jupyter Notebook or Jupyter Lab?

You can use .NET Interactive as a standard Jupyter kernel and use it with Jupyter Notebook, JupyterLab, and other Jupyter frontends. Instructions for registering it as a Jupyter kernel can be found [here](https://github.com/dotnet/interactive/blob/main/docs/NotebookswithJupyter.md). 

### How do I load a NuGet package?

You can use the `#r nuget` directive within a C# or F# cell to load NuGet packages into a notebook. You can read more details [here](https://github.com/dotnet/interactive/blob/main/docs/nuget-overview.md).

### How do I load a NuGet package from a custom package feed?

Yes. Custom package feeds are supported via the `#i nuget` directive. You can read more details [here](https://github.com/dotnet/interactive/blob/main/docs/nuget-overview.md).

### How do I load a NuGet package from a private package feed?

This is not directly supported by .NET Interactive, but you can authenticate to a NuGet feed by storing a PAT in your user-level nuget.config. You can read more about this approach [here](https://learn.microsoft.com/en-us/azure/devops/artifacts/nuget/nuget-exe?view=azure-devops). 

### Can I directly load a .NET assembly?

You can directly load a .NET assembly in a C# or F# cell by using the `#r` compiler directive:

```csharp
#r "/path/to/assembly.dll"
```

### Can I use a notebook to run code from a C# or F# project?

Directly running code from the C# or F# project isn't currently supported, but the scenario is [under consideration](https://github.com/dotnet/interactive/issues/890) and feedback is welcome.

There are a couple of approaches that might work, depending on what you're trying to do.

* You can run code from a specific file directly using the `#!import /path/to/file` magic command, which can understand a number of different file formats. The contents of the file will be run directly. If the file is a `.cs` file it will attempt to run it using C# Script, and the dialect differences might prevent the file from compiling. (For example, since C# Script doesn't support namespaces, the file will not be able to include them.) F# files (`.fs`) tend to be more likely to be runnable in this way.

* You can call into assemblies built from your project code by using the `#r` compiler directive in a C# or F# cell.

### How do I load a JavaScript library?

You can load a JavaScript library using `require`.

[Example](../samples/notebooks/javascript/Plotly%20with%20RequireJS.ipynb)

JavaScript package managers are not currently supported, however.

### Can I define my own magic commands?

You can define new magic commands using the .NET Interactive extension APIs.

[Example](extending-dotnet-interactive.md#adding-magic-commands)

### How do I share variables between subkernels?

Not all kernels support variable sharing, but for those that do, you can use the `#!share` magic command to pull a variable from one kernel into another.

[Example](variable-sharing.md)

### How do I display a value?

There are a few ways to display values, and some differences depending on which language you're using.

#### Console output

The most universal approach is to write to the console, though this will result in plain text output that might be less interesting in some cases. (Return values and display values, described below, can be richly formatted using .NET Interactive [formatters](formatting.md).)

In C# and F# you can call `Console.WriteLine`.

<img alt="image" src="https://user-images.githubusercontent.com/547415/210457457-514887d0-55f7-4fcf-a94f-d1c7a45cadaf.png" width="60%">

In PowerShell, an unpiped expression will produce console output by default, so all you have to do is this:

<img alt="image" src="https://user-images.githubusercontent.com/547415/210457374-4a36ce9f-5375-44a8-8e67-a0003c974385.png" width="60%">

In JavaScript, you can call `console.log`.

<img alt="image" src="https://user-images.githubusercontent.com/547415/210458819-f2744884-ccaa-4d08-99bc-910929b9b1e7.png" width="60%" >

#### Return values

You can also return a value in order to display it. C# Script and F# have a similar syntax for this, which is a trailing expression. This is the common syntax for returning a value in F#. In C# Script, it's a syntax that is not valid in common C#.

<img alt="image" src="https://user-images.githubusercontent.com/547415/210460177-e94c082f-b689-47b1-ae9c-4e7bf5a5e12f.png" width="60%" >

#### Display APIs

Since a return statement is only valid once in a given submission, .NET Interactive also provides an extension method that can be used to display any object. This can be called more than once in a single submission.

<img alt="image" src="https://user-images.githubusercontent.com/547415/210459216-76c43f16-6d28-4cd7-8192-d37230085ba1.png" width="60%" >

In PowerShell, you can use `Out-Display` to pipe values through the display helper.

<img width="719" alt="image" src="https://user-images.githubusercontent.com/547415/210459686-94209e69-acf3-4837-9d52-1af72b743654.png">

### Can I change how a value is displayed?

The formatter APIs allow extensive control over the way that outputs (including notebook cell return values and values displayed using the `Display` method) are displayed. All type-specific formatting, as well as the differences between notebook outputs (typcally HTML) and REPL outputs (typically plain text), is powered by these APIs.

You can read more about the formatter APIs [here](formatting.md).

### How do I prompt a user for input?

There are a few ways to prompt a user for input in a notebook.

You can prompt for input using a token such as `@input:prompt_name` within any magic command:

<img width="75%" alt="image" src="https://user-images.githubusercontent.com/547415/213321240-2e332e83-1b9e-42cd-a951-931006def638.png">

(As you can see from the file picker in the screenshot, input prompts can be type-specific.) 

There is also an API that can be called directly to prompt users for input. Here's an example in C#:

```csharp
using Microsoft.DotNet.Interactive;

var input = await Kernel.GetInputAsync("Pick a number.");
```

When you run this code in Polyglot Notebooks (as shown in the screenshot below), a text input prompt appears at the top of the Visual Studio Code window.

<img alt="image" src="https://user-images.githubusercontent.com/547415/210603522-8738fa01-105d-4d0f-93cd-976da0a73a6c.png" width="60%" >

You can read more about input prompts [here](input-prompts.md).

### I need to use secrets in a notebook. How do I do it without saving the secret in the file?

User input prompts are a good way to provide secrets to a notebook without risk of storing them in the file. Similar to the 

```csharp
using Microsoft.DotNet.Interactive;

var input = await Kernel.GetPasswordAsync("Pick a number.");
```

When you run this code in Polyglot Notebooks (as shown in the screenshot below), an input prompt appears at the top of the Visual Studio Code window. When the user types into this prompt, the text is masked.

<img alt="image" src="https://user-images.githubusercontent.com/547415/210673597-2603b6e5-ecba-4e4d-abc4-dbeba28df9c4.png" width="60%" >

### A notebook cell is stuck. How do I stop it?

When you run a cell, the run button (▶️) changes to a stop button (⏹️). Pressing the stop button will attempt to stop the running cell, but it might not always work. Some programs can't be interrupted easily. If the stop button doesn't work, then you can restart the kernel using the `Polyglot Notebook: Restart the current notebooks kernel` command from the command palette.

### If I have some code stored in a variable, can I run it?

Yes, you can use the .NET Interactive APIs to send code to any kernel by name.

Here's an example in C#:

```csharp
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

var code = @"Console.WriteLine(""Hello!"");";

await Kernel.Root.FindKernelByName("csharp")
            .SendAsync(new SubmitCode(code));
```

### Is there a way to run a notebook from the command line?

The [.NET REPL](https://github.com/jonsequitur/dotnet-repl) has a number of features relating to command line automation with notebooks. The GitHub [project page](https://github.com/jonsequitur/dotnet-repl) has more details. 

[_This is an experimental feature that might be added to the core .NET Interactive product in the future._]

### How can I test my notebooks?

One approach to automated testing of notebooks is to use [.NET REPL](https://github.com/jonsequitur/dotnet-repl). The following example shows how to run a notebook headlessly and output a `.trx` file containing the results:

```console
dotnet repl --run /path/to/notebook.ipynb --output-format trx --output-path /path/to/results.trx --exit-after-run 
```

More details can be found on the GitHub [project page](https://github.com/jonsequitur/dotnet-repl).

[_This is an experimental feature that might be added to the core .NET Interactive product in the future._]

### Can I call one notebook from within another?

Yes. You can use the `#!import` magic command to load and run a number of different file types within a notebook, including `.ipynb` and `.dib`. Both of these file formats support polyglot, so notebooks imported this way can include cells in various languages.

You can also use `#!import` to run single-language files within a notebook, including `.cs`, `.csx`, `.js`, and `.ps1` files. 

You can read more about `#!import` [here](import-magic-command.md).

### How can I add other languages to a notebook?

Yes. The .NET Interactive [extensibility APIs](extending-dotnet-interactive.md) allows for NuGet packages to add new subkernels at runtime. This is how SQL and KQL support are implemented.

### Can I add cells to a notebook programmatically?

Yes, in Polyglot Notebooks you can programmatically add a new cell by sending the `SendEditableCode` command.

```csharp
using Microsoft.DotNet.Interactive;
using Microsoft.DotNet.Interactive.Commands;

var command = new SendEditableCode(
    "csharp", 
    "Console.WriteLine(\"Hello!\");");

var input = await Kernel.Root.SendAsync(command);
```

When you send this command, a new cell will be appended to the notebook with the specified content and selected kernel:

<img  alt="image" src="https://user-images.githubusercontent.com/547415/210672882-9825764b-f2dc-4f57-9d9e-21cefc86bed5.png" width="60%">

This command is not currently supported in other notebook frontends such as JupyterLab.

## Troubleshooting

### A cell runs forever

It is common for a notebook's kernel to get into a stuck state. Maybe you tried to load too much data or accidentally ran code containing an infinite loop. This happens often enough that a way to restart the kernel is a feature of most notebook providers.

In Polyglot Notebooks, you can restart the kernel by running the `Polyglot Notebook: Restart the current notebook's kernel` command from the Command Palette. Note that after you do this, you'll need to rerun the notebook's cells, including reloading packages using `#r`.

### Nothing happens when running a cell

Sometimes VS Code updates have been applied or are pending and the Polyglot Notebooks extension needs to be updated. If things aren't working, here are a few things to check.

If you see the following badge on the settings icon in the lower left corner of VS Code, it means there might be an update pending:

<img width="48" alt="image" src="https://user-images.githubusercontent.com/547415/224158533-c01456c5-0759-46ac-a8c1-317586974a16.png">

When you click it and see a `Restart to Update` message in the menu, then VS Code needs an update.

<img width="299" alt="image" src="https://user-images.githubusercontent.com/547415/224158995-0d5864cc-57b3-416a-95bb-3d18ea42c7bb.png">

You might also see, including after a VS Code update has been applied, that the Polyglot Notebooks extension requires a reload.

<img width="360" alt="image" src="https://user-images.githubusercontent.com/547415/224161370-1c628967-ae0e-42b2-9c64-e3c1d7756f0b.png">

If you're still seeing issues running code after checking for updates, please [open an issue](https://github.com/dotnet/interactive/issues/new/choose). 

### `No formatter is registered for MIME type [____]`

This error indicates that a call to a .NET Interactive formatting API such as `Display`, or in some cases `#!share` or `#!set`, failed because formatting the specified object using the requested MIME type isn't available. You can add formatters for additional MIME types using the formatter registration APIs. More details about these APIs can be found [here](formatting.md).

### `No renderer could be found for mimetype "[____]", but one might be available on the Marketplace`

The error is shown because a formatted value from the .NET Interactive kernel has been formatted using a MIME type that VS Code doesn't know how to render. VS Code can only render notebook outputs in MIME types for which there's an installed [notebook renderer](https://code.visualstudio.com/api/extension-guides/notebook#notebook-renderer). Notebook renderers are independent of the kernel and can be [installed from the Visual Studio Marketplace](https://code.visualstudio.com/api/extension-guides/notebook#notebook-renderer).  

### `Unrecognized parameter name '--kernel-name'` when using `#!connect mssql`

This error occurs when the version of `Microsoft.DotNet.Interactive.SqlServer` doesn't match the version of .NET Interactive you're running. When using a wildcard version specifier like `*-*`, you might load a version that's incompatible with your current Polyglot Notebooks extension.

To fix this:

1. Run `#!about` in a cell to check your .NET Interactive version.

2. Use the **Library version** number (the part before the `+`) to get the matching package version. For example, given this output from `#!about`:

```console
.NET Interactive

© 2020-2025 Microsoft Corporation

Version: 1.0.617701+fb2fd8022ab96c55fbaf34d5e1c8c61cb01690fc

Library version: 1.0.0-beta.25177.1+fb2fd8022ab96c55fbaf34d5e1c8c61cb01690fc
                 ^----------------^
```

The underlined section is the version that should work with your current version of .NET Interactive, e.g.:

```csharp
#r "nuget:Microsoft.DotNet.Interactive.SqlServer,1.0.0-beta.25177.1"
```

Replace `1.0.0-beta.25177.1` with the Library version number you see in your `#!about` output.

**Important:** The `#r nuget` directive and the `#!connect mssql` command must be in separate cells. The notebook validates syntax before running code, but the `#!connect mssql` command is only recognized after the package loads.

**Note:** Pre-release versions of Polyglot Notebooks use different package versions than the stable release. Pre-release packages aren't available on nuget.org but can be loaded from the Azure DevOps feed: https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-tools/nuget/v3/index.json

### Diagnostic logs

You can enable diagnostic logging by editing the Polyglot Notebooks extension's settings for `Kernel Transport Args` and adding the following command line arguments:

```diff
"dotnet-interactive.kernelTransportArgs": [
    "{dotnet_path}",
    "tool",
    "run",
    "dotnet-interactive",
    "--",
    "[vscode]",
    "stdio",
    "--working-dir",
    "{working_dir}",
+   "--log-path",
+   "c:\\temp\\your-log-folder-name",
+   "--verbose",
],
```

![Image](https://github.com/user-attachments/assets/cf75e69f-177a-4275-9f44-88dc0c2571f5)