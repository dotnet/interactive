# .NET Interactive Architectural Overview 

The kernel concept in .NET Interactive is a component that accepts commands and produces outputs. The commands are  typically blocks of arbitrary code, and the outputs are events that describe the results and effects of that code. The `Kernel` class represents this core abstraction.

A kernel doesn't have to run in its own process. The default `dotnet-interactive` configuration runs several kernels in one process, enabling scenarios such as language-switching and .NET variable sharing. But one or more kernels can also run out-of-process, which will be transparent from the point of view of someone using it.

The `dotnet-interactive` tool also provides a number of protocols, including the [Jupyter message protocol](https://jupyter-client.readthedocs.io/en/stable/messaging.html) and a JSON protocol that can be accessed over either standard I/O or HTTP. These multiple protocols allow the core set of capabilities to be fairly portable.

![image](https://user-images.githubusercontent.com/547415/94998579-9c7adc80-0567-11eb-8b90-aa64a790ca01.png)


## Commands and events

All communication with a kernel takes place through a sequence of commands and events. The typical sequence starts with a command being sent to the kernel, which will reply with one or more events. The terminating event will always be either `CommandSucceeded` (if everything completed successfully) or `CommandFailed` (if there was a compilation error or runtime exception), but this will usually be preceded by one or more other events describing the results of the command. 

The most common command is `SubmitCode`, which is used when a block of code is sent to the kernel for execution. A code submission is created each time you run a notebook cell. But a single submission may in fact generate multiple commands.

Consider the following submission to a C# kernel:

```csharp
#!time
Console.WriteLine("Hi!");
```

This submission will actually be broken into two commands, a `SubmitCode` for the `Console.WriteLine` call as well as an internal `DirectiveCommand` for the `#!time` magic command. 

When this splitting occurs, the API still only returns a single terminating `CommandSucceeded` or `CommandFailed` event. Programmtically, you don't need to be concerned with whether a submission is going be split, but understanding this mechanism can be helpful, for example when implementing your own middleware behaviors.

You can see some additional examples of command and event interactions in the following diagram, illustrating different kinds of output as well as the behavior of a middleware component (for the `#!time` magic command) augmenting the behavior of a code submission by emitting an additional `DisplayedValueProduced` event.

![image](https://user-images.githubusercontent.com/547415/85328568-ce1eda80-b485-11ea-8d6e-a821dfe5db62.png)

## Nested Kernels

In the standard configuration, .NET Interactive uses multiple, nested kernels. These kernels share a common set of interfaces which allow them to be composed into different kinds of pipelines. This is the basis for supporting multiple languages, among other features. A user of a .NET Interactive notebook can specify the language for a code submission by prefixing a block of code with a [magic command](magic-commands.md) such as `#!csharp`, `#!fsharp`, or `#!pwsh`, or by using the language selector in the lower right corner of a Visual Studio Code notebook cell.

<img src="https://user-images.githubusercontent.com/547415/82159474-276e6b00-9843-11ea-8ec0-f3f5bcee7547.png" width="40%">

The language-selection magic commands will even allow you to submit code for multiple languages in a single notebook cell. Once again, the submission will be split into several commands, just like in the `#!time` example above. Consider this submission:

```csharp
#!csharp
Console.WriteLine("Hello from C#!");
#!fsharp
"Hello from F#!" |> Console.WriteLine
```

Even though this will initially be sent as a single `SubmitCode` command, it will be split into two different `SubmitCode` commands, each targeting the appropriate subcommand.

The work of routing these commands is done by the `CompositeKernel` class, which wraps a number of subkernels. Here are some examples: 

![image](https://user-images.githubusercontent.com/547415/85328679-ff97a600-b485-11ea-839c-ebc65b0f6472.png)

Note that while the composite configuration is the defaut when using the `dotnet-interactive` tool via Visual Studio Code or Jupyter, the .NET Interactive [NuGet packages](../README.md#Packages) let you create other configurations. For example, you might provide a single-language embedded scripting experience using the C# kernel by itself, or you might provide multiple F# kernels each preconfigured to run code on a different processor.

## Middleware

The submission splitting behavior is implemented using .NET Interactive middleware. Each kernel has its own configurable middleware pipeline. You can think of the middleware pipeline as a chain of functions concatenated together. Each function in the chain can choose to perform operations before and/or after calling the continuation that will invoke the next function, or can opt to short-circuit the whole pipeline by not calling the continuation at all. This structure allows custom middleware to be added that can perform arbitrary tasks. Examples include executing additional commands (such as in the command-splitting examples), catching exceptions thrown by the inner operations, outputting timing and diagnostics, checking credentials, and more.

