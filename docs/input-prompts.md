# Input prompts

In a notebook, it's often useful to be able to ask the user for certain values. Query parameters, authentication tokens, and file paths are common examples of information used to parameterize a notebook in order to reuse it with different data or among different users.

There are a few different ways to avoid having to hard-code such values in your notebooks.

## Input prompts in magic commands

Within any magic command, you can prompt the user for a parameter value using an `@input` token in place of a specific value. This will work with any magic command. Here's a simple example using the [`#!set` magic command](https://github.com/dotnet/interactive/blob/main/docs/variable-sharing.md#set-a-variable-from-a-value-directly):

```console
#!set --name myVariable --value @input
```

When this cell is run, an input prompt is displayed at the top of the VS Code window.

<img width="60%" alt="image" src="https://github.com/user-attachments/assets/94f7c1b1-6b7e-4629-8c28-4ea9a0725402">

If you fill in a value and press `Enter`, then the value is passed to the corresponding magic command parameter. In this example, we can see the entered value afterwards in the Variables View:

<img width="60%" alt="image" src="https://github.com/user-attachments/assets/719104a5-2da8-45de-8694-b9579ab5f176">

There are a few ways that you can customize the behavior of an `@input` or `@password`. (The following examples use `@input` but all of these will also work with `@password`.)

### Specifying a prompt

You can customize the user prompt using either of the following syntaxes. Both examples show the same prompt:

```console
#!set --name myVariable --value @input:"Please enter a value"
#!set --name myVariable --value @input:{"prompt": "Please enter a value"}
```

<img width="60%" alt="image" src="https://github.com/user-attachments/assets/a0093ab6-17f5-4450-9e39-d15bbd154566">

The first example is a shorthand for the second example. Other ways of customizing the behavior of an `@input` will follow the pattern of the second, using JSON to pass parameters.

### Saving a value

Sometimes the value of an `@input` is something that's not going to change for a given user over time. But closing and reopening the notebook or restarting the kernel causes in-memory state to be lost, and if the value you're going to use for a given `@input` is going to be the same every time, you can choose to save that value so that you don't have to keep reentering it.

To reuse the entered value, you can specify a `saveAs` property in the `@input` parameters:

```console
#!set --name myConfigFile --value @input:{"saveAs": "widget configuration"}
```

When this `@input` is run for the first time, it will produce a prompt as usual.

<img width="60%" alt="image" src="https://github.com/user-attachments/assets/895819b5-2ce2-444f-bbde-fbd2c35b4bb4" />

The output however will indicate that the value has been stored for future reuse, and includes instructions for resetting this value should you want to change it later.

<img width="60%" alt="image" src="https://github.com/user-attachments/assets/0ca1c00d-659c-4ca9-bfc8-cf6da8500342" />

The next time you run a cell containing an input prompt with `saveAs` set to that name, no prompt will be shown. The previously saved value will be used and a reminder message will be shown to indicate this:

<img width="60%" alt="image" src="https://github.com/user-attachments/assets/78fe7f42-acee-495f-8a85-2e98906ced78" />

This feature is powered by the PowerShell [SecretManagement and SecretStore modules](https://learn.microsoft.com/en-us/powershell/utility-modules/secretmanagement/overview?view=ps-modules). It will store the values securely on the machine where the PowerShell kernel is running. For most use cases that will be your local machine but, for example, if you're running a notebook using GitHub CodeSpaces, the value will be stored on the VM where the dev container is running, which means it will be gone when the VM is recycled.

### Type hints

All values provided for `@input` tokens are strings, but sometimes a more ergonomic UI is available for the kind of string you're expecting to receive within a notebook. For example, if what you're expecting is a valid path to an existing file, a file picker is going to provide a better experience than a plain text input.

This is supported through the `type` property on the `@input` parameters.

```console
#!set --name myConfigFile --value @input:{"type": "file"}
```

In this example, rather than the text input box at the top of the VS Code window, a file dialog is displayed.

<img alt="image" src="https://github.com/user-attachments/assets/d0271eaa-fd54-4b1b-a7db-6ce432aee10e" width="60%" >

The input types supported for single inputs are currently limited to `text` and `file` but other types can be used when multiple inputs are present in a single magic command, as described in the next section.

### Multiple inputs

When there are multiple `@input` or `@password` tokens in a single magic command, a form is shown in the cell's output area with fields for each one. Here's an example:

```console
#!set --name @input:{"prompt": "Please enter a name for the color"} --value @input:{"type": "color", "prompt": "Pick a color. Any color."}
```

<img alt="image" src="https://github.com/user-attachments/assets/c161a4f4-611a-40e0-bcec-8139e30e958d" width="60%" >

Note that all of the preceding `@input` customizations are available. In this example, you can see that a color picker was shown because the `type` property of the `@input` widget was set to `color`. 

All standard [HTML input types](https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input#input_types) are supported.

## Input prompts using .NET code

There are also ways to use .NET code directly to prompt users for input, rather than by using a magic command. Here's an example in C# for prompting for a single input:

```csharp
using Microsoft.DotNet.Interactive;

var input = await Kernel.GetInputAsync("Pick a number.");
```

The resulting prompt looks the same as the one that's shown when using an `@input` in a magic command:

<img alt="image" src="https://user-images.githubusercontent.com/547415/210603522-8738fa01-105d-4d0f-93cd-976da0a73a6c.png" width="60%" >

You can also provide a type hint using this API, which the web view can use to show a more specific UI.

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

### Multiple inputs

You can request multiple inputs at once using the `Kernel.RequestInputsAsync` method. (You can also use this to request a single input if you want the input fields to appear in the notebook's output cell). This produces the same kind of input form as you see when you run a magic command containing multiple `@input` tokens.

<img width="60%" alt="image" src="https://github.com/user-attachments/assets/70b4afdc-82f9-4b98-8c10-c701ef72b935">

### Passwords and other secrets

Some notebook inputs are sensitive and you don't want them to be saved in a readable form in your notebook's code cells or outputs.

You can prompt for input using `Kernel.GetPasswordAsync` 

```csharp
using Microsoft.DotNet.Interactive;

var input = await Kernel.GetPasswordAsync("Please provide a connection string.");
```

The resulting prompt masks the text that the user types:

<img width="60%" alt="image" src="https://user-images.githubusercontent.com/547415/211089804-3807d3cb-b050-49dd-a4ef-e38accd7773b.png">

### Inputs from command line parameters

[_This section describes an experimental feature that might be added to the core .NET Interactive product in the future._]

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

