# Input prompts

In a notebook, it's often useful to have certain values provided from outside. Query parameters, authentication tokens, and file paths are common examples of information used to parameterize a notebook in order to reuse it for different data or among different users. 

There are a few different ways to avoid having to hard-code such values in your notebooks.

## Input prompts within magic commands

Within any magic command, you can prompt for input by prefixing a token with `@input:`. For example, `@input:xyz` will prompt the user with the text `xyz`. The input will replace the text `@input:prompt_text` (The following examples use the `#!value` command but this syntax works with any magic command. For more information about using `#!value`, you can read about the value kernel [here](value-kernel.md)).

In most cases, the input prompt will be a text field. Here's an example showing an input prompt in Polyglot Notebooks:

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/211088346-bb944997-c79b-4d71-96b3-bf7be8747d8d.png">

Input prompts look a little different JupyterLab, but they work the same way:

<img width="55%" alt="image" src="https://user-images.githubusercontent.com/547415/211088510-fc31eed8-8ac6-4fbf-af1c-7ef61c826eab.png">

In some cases, input prompts can present UI that's more specific to the type that the magic command is expecting for a given argument. One example is `#!value --from-file`. Since it expects a file input, the Polyglot Notebooks UI will show a file chooser instead of a text box:

<img alt="image" src="https://user-images.githubusercontent.com/547415/210602472-22bbf35c-316b-4c32-b891-58b226853301.png" width="60%" >

## Input prompts via .NET code

There are also methods that can be called directly in .NET code to prompt users for input. Here's one example in C#:

```csharp
using Microsoft.DotNet.Interactive;

var input = await Kernel.GetInputAsync("Pick a number.");
```

The resulting prompt looks the same as the one that's shown when using the `@input:` prefix in a magic command:

<img alt="image" src="https://user-images.githubusercontent.com/547415/210603522-8738fa01-105d-4d0f-93cd-976da0a73a6c.png" width="60%" >

You can also provide a type hint using this API, which the frontend can choose to use to provide more specific UI.

```csharp
using Microsoft.DotNet.Interactive;

var input = await Kernel.GetInputAsync(
    "Please provide a connection string.",
    typeHint: "file");
```

If the type hint is one that's understood by the frontend, you'll see the appropriate UI:

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/211090654-efdab0dc-6f1e-4c93-9fdd-4fbaf3c37ec7.png">

Otherwise, it will fall back to the simple text input:

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/211090750-f01a8379-b506-46aa-8b6b-4c22d57abfbf.png">

## Passwords and other secrets

Some notebook inputs are sensitive and you don't want them to be saved in a readable form in your notebook's code cells or outputs. 

You can prompt for input using `Kernel.GetPasswordAsync` 

```csharp
using Microsoft.DotNet.Interactive;

var input = await Kernel.GetPasswordAsync("Please provide a connection string.");
```

The resulting prompt masks the text that the user types:

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/211089804-3807d3cb-b050-49dd-a4ef-e38accd7773b.png">

## Inputs from command line parameters

[_This is an experimental feature that might be added to the core .NET Interactive product in the future._]

The capability for a notebook to prompt a user for input solves a number of problems. But in automation scenarios (such as using a notebook as an automation tool or running tests of notebooks), there is no user present to respond to the prompt.

The [.NET REPL](https://github.com/jonsequitur/dotnet-repl) has features for running notebooks from the command line, with no UI or user present. In order to be able to provide values for known inputs, .NET REPL can identify `@input:`-prefixed tokens in magics and allows you to pass values for these inputs at the command line. 

You can find out what parameters are required by a notebook by running the following command and passing the path to the notebook. (This works for both `.ipynb` and `.dib` files.)

```console
 dotnet repl describe /path/to/notebook.ipynb
```

The `dotnet repl describe` command will display the parameters needed by the notebook and show examples of how to pass them using command line options:

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/211161842-39437eba-5da2-4b02-b13a-3a0848fe4d45.png">

The following command will run a notebook (called `parameters.ipynb`) and write the results to a new notebook (called `parameters-output.ipynb`). 

```console
 dotnet-repl --run .\parameters.ipynb --exit-after-run --output-path parameters-output.ipynb --input parameter1="value one" --input parameter2="value two"
```

Note that the `--input` option is specified more than once.

For more information about usng .NET REPL, see the GitHub [project](https://github.com/jonsequitur/dotnet-repl) page.

