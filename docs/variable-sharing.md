# Variable sharing

The .NET Interactive kernel enables you to write code in multiple languages within a single notebook. In order to take advantage of the different strengths of each language, you'll find it useful to share data between them. By default, .NET Interactive supports a number of different languages and most of them allow sharing using the `#!set` and `#!share` magic commands. The documentation will focus on the use of `#!set`, which provides all of the capabilities of `#!share` as well as some additional ones.

> *&nbsp;_The `#!share` magic command has been in place since the earliest days of .NET Interactive, while `#!set` is newer and provides a superset of the capabilities of `#!share`. Since `#!set` has richer capabilities and is a little more readable, you might prefer it to `#!share`. It's easy to rewrite a `#!share` command to use `#!set`. Here's an example of `#!share` usage:_
> ```
> #!share --from javascript jsVar --as csVarFromJs
> ```
>
> _You can do the equivalent using `#!set` like this:_
>
> ```console
> #!set --name csVarFromJs --value @javascript:jsVar
> ```
> 

The `#!set` magic command's `--name` option provides the name of the variable you'll be creating. The `--value` option provides its value. While `#!share` only allows you to share an existing value, `#!set` also allows you to declare one in-place. This is the primary way in which `#!set` provides more functionality than `#!share`.

## Set a variable from a value directly

The simplest way to specify a value using `#!set` is to specify it directly in the magic command. The other ways you can use `#!set` build on this.

```
#!set --name url --value https://example.com
```

If the value contains spaces, you can put quotes around it so that it will be understood as a single string:

```
#!set --name words --value "one two three"
```

Of course, most languages already support setting variables, so specifying a value using `#!set` in a kernel for a language like C# or JavaScript is redundant. But some languages don't support variables, or don't support them in a long-running scope. For example, .NET Interactive's SQL and KQL kernels store values created using `#!set` within the kernel and send them to the server with each query. In cases like these, specifying the values inline in the `#!set` magic command can be useful.

> _Note: `#!share` does not support setting a value directly in this way._

## Share a variable between kernels

When you have a variable in one kernel that you'd like to use in another, you can share it using the `#!set` magic command. You must give the incoming value a name using the `--name` option, and specify a value using the `--value` option. The `--value` option should specify both the kernel where the variable is coming from and the name of the variable in that kernel, in the format `@SourecKernelName:SourceVariableName`. The `#!set --value` option provides completions in this format, which can help you identify which variables are available for sharing:

<img width="509" alt="image" src="https://user-images.githubusercontent.com/547415/225192121-5c9b0461-aef4-4aa8-a027-babd8c564788.png">

When you run the `#!set` command, it creates a copy of the source variable in the kernel where you run it. In the following screen shot, running the command `#!set --value @csharp:words --name fromCsharp` in the JavaScript kernel copies the C# variable `words` to a JavaScript variable called `fromCsharp`. 

<img width="510" alt="image" src="https://user-images.githubusercontent.com/547415/225191987-8be846fe-6c18-44c7-b6b6-62e8b64b9407.png">

By default, this copy is created using the `application/json` MIME type. This means that the C# `words` variable is serialized to JSON and that JSON is then parsed into an object in JavaScript, which is assigned to the `fromCsharp` variable. (If the value can't be serialized, you will see an error. The formatters can be configured to work around this. More information can be found [here](formatting.md).)

### Variables View

Another way to share variables between kernels can be found in the Variables View. You can open it by clicking the `Variables` button at the top of the notebook, or by selecting `Polyglot Notebook: Focus on Variables View` from the Command Palette. 

<img width="633" alt="image" src="https://user-images.githubusercontent.com/547415/225194981-afc2b8c1-783b-4010-88c8-04924379aef0.png">

In the `Actions` column, there is a share button. 

<img width="633" alt="image" src="https://user-images.githubusercontent.com/547415/225195321-6bb635fa-3239-4056-925d-1ef4b0fffb96.png">

When you click this button, you'll be asked which kernel you want to share the variable with.

<img width="346" alt="image" src="https://user-images.githubusercontent.com/547415/225195702-3ceb4944-4b75-44ff-b7db-062ca95e4b16.png">

Once you make a selection, a cell will be added to your notebook that is prefilled with the appropriate `#!set` magic command.

<img width="500" alt="image" src="https://user-images.githubusercontent.com/547415/225195652-58474fbc-744f-44b4-8bd9-66e7f8bf5acc.png">

## Set a variable from user input

You can set a value directly from user input by using an `@input` prefix in your magic command. Here's an example:

```
#!set --name url --value @input:"Please enter a URL"
```

When you run this, an input prompt will appear at the top of the VS Code window. The value you enter there will be stored in the variable specified by the `--name` option.

If you'd like the user input to be masked in the UI because it contains a secret you don't want to display on your screen, you can use a `@password` prefix instead of the `@input` prefix:

```
#!set --name topSecret --value @password:"Please enter the password"
```

The ability to request user input via `@input` and `@password` prefixes isn't limited to the `#!set` magic command. You can read more about these features [here](./input-prompts.md).

## MIME types

When a variable is shared between subkernels in .NET Interactive, it must typically be converted into a string representation of some kind. This is because many of the subkernels in .NET Interactive run in different processes. For example, the core kernel runs in its own .NET process while the Polyglot Notebooks extension runs in a VS Code process. You can also run subkernels on remote machines. Subkernels can be implemented on different platforms as well, for example .NET versus JavaScript. So while there's support for sharing variables by reference between .NET languages when they share a process, the main use cases for sharing involves some form of serialization. The serialization format is referred to by a MIME type, and both the `#!set` and `#!share` magic commands allow you to specify it using the optional `--mime-type` option. 
If you don't specify the `--mime-type` option, then the default MIME type used for variable sharing is `application/json`. This means that the requested variable will be serialized as JSON by the source kernel and then, optionally, deserialized by the destination kernel. For .NET-based kernels, serialization is performed using `System.Text.Json`. The deserialization strategy used in .NET-based destination kernels is the following:

 Source JSON type   | Destination .NET type   
--------------------|-------------------------
`boolean`           | `System.Boolean`
`number`            | `System.Double`    
`string`            | `System.String`  
_other_             | `System.Text.Json.JsonDocument`  

Converting a variable into the specified MIME type is done using the .NET Interactive formatter APIs, which can be customized to your needs. You can read more about these APIs [here](formatting.md).

## Sharing by reference

In certain specific cases, variables can be shared by reference for reference types. This comes with a number of caveats. 

* The source and destination kernels must both be running in the same process.
* The source and destination kernels must be CLR-based (e.g. C#, F#, PowerShell).
* If you're using `#!set`, reference sharing is only enabled when using the `--byref` option. 
* If you're using `#!share`, reference sharing is the default behavior and is disabled when the `--mime-type` option is used. 

One consequence of this is that if you share a mutable object, changes to its state will be immediately visible across subkernels, which is not the case with the default serialization-based sharing.

<img width="498" alt="image" src="https://user-images.githubusercontent.com/547415/225198698-393e14e4-0b6a-49b4-bbb2-0477b8393839.png">

## Direct data entry with `#!value`

It's common to have text that you'd like to use in a notebook. It might be JSON, CSV, XML, or some other format. It might be in a file, in your clipboard, or on the web. The `#!value` magic command is available to make it as easy as possible to get that text into a variable in your notebook. An important thing to know is that `#!value` is an alias to a specialized subkernel designed just to hold values. This means that once you store something in it, you can access it from another subkernel using `#!set` or `#!share`.

There are three ways to use `#!value` to get data into your notebook session:

### 1. From the clipboard

 The simplest way to use `#!value` is to paste some text into the cell. The text will be stored as a string, but unlike using a `string` literal in C#, F#, or PowerShell, there's no need to escape anything.

<img src="https://user-images.githubusercontent.com/547415/89252742-81273b80-d5cf-11ea-8769-6d51eaa0669f.png" width="40%">

### 2. From a file

If the data you want to read into your notebook is stored in a file, you can use `#!value` with the `--from-file` option:

<img src="https://user-images.githubusercontent.com/547415/89600459-fdf82680-d816-11ea-8ba6-1d5ec4e2a7e7.png" width="40%">

### 3. From a URL

You can pull data into your notebook from a URL as well, using the `--from-url` option. 

<img src="https://user-images.githubusercontent.com/547415/89846563-66584800-db36-11ea-8a17-57a48b45b0f1.png" width="40%">

## Specifying a MIME type

Regardless of which of these approaches you use, you can additionally choose to display the value in the notebook at the time of submission by using the `--mime-type` option. This accomplishes a few things. If your notebook frontend knows how to display that mime type, you can see it appropriately formatted:

<img src="https://user-images.githubusercontent.com/547415/89252758-8ab0a380-d5cf-11ea-9873-78d7060f8157.png" width="40%">

This also causes the value to be saved in your `.ipynb` file, something that would not otherwise happen.
