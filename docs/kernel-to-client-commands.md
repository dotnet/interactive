# Kernel-to-client communication with client-side commands

When you execute code in a notebook, it normally follows a particular pattern: client side code handles user input, which results in a command being sent to the kernel, which will send one or more messages back to the client reporting progress and the final output. However, there are some scenarios where it is useful for the kernel to be able to initiate communication. To enable these, .NET Interactive supports a bidirectional model in which the kernel process can send commands to the client.

This is useful if execution of notebook cell starts an ongoing process. For example, imagine a notebook controlling a motorised robot with sensors around its perimeter able to report when it has come into contact with something. You could imagine a cell which, when run, produces an output containing a scrolling list, with a new line being shown each time one of these sensors detects a change. This ongoing reactive display does not fit well with the model in which all kernel-to-client notifications are sent within the scope of a particular command, because it would require the command never to finish. And since .NET Interactive currently uses a model in which commands are executed sequentially, such an ongoing activity would prevent any other cell in the notebook from running. This kind of output really needs the kernel process to be able to send messages to the client whenever necessary, and outside the context of any particular client-to-kernel command.

## Client-side kernel

.NET Interactive already supports the idea of a composite kernel which can route commands to other kernels, some of which might be remote. This offers a natural way for code running in the kernel process to initiate communications with the client: the client can host its own kernel, which is accessible from the main kernel process as a remote kernel.

This enables kernel-to-client communications to follow exactly the same model as client-to-kernel (and, for that matter, kernel-to-remote-kernel) communications.

To connect the .NET Interactive kernel to the client-side kernel, run a cell with the following directive:

```
#!connect client --kernel-name notebook
```

This registers an additional kernel with the root .NET kernel. The `client` argument indicates that this is to be a client-side kernel. The `--kernel-name` argument supplied here means that any commands with a target kernel name of `notebook` will now be directed to that kernel.

## Defining and registering custom command types

To define a custom command, define a .NET class that inherits from `KernelCommand`, e.g.:

```cs
using Microsoft.DotNet.Interactive.Commands;

public class MyToClientCommand : KernelCommand
{
    public MyToClientCommand(string data) : base("notebook")
    {
        this.Data = data;
    }
    public string Data { get; }
}
```

Note that the constructor here passes `notebook` as a base constructor argument. The base `KernelCommand` constructor uses this to set the `TargetKernelName` property, and by passing `notebook` here, we ensure that commands of this type will be directed to the client-side kernel named `notebook` registered earlier with the `#!connect` directive.

The .NET kernel needs to be aware of any command types so as to be able to serialize them correctly. Registering a handler for a custom command in .NET (by calling `Kernel.Current.RegisterCommandHandler<MyCustomCommand>`) has this effect, but since .NET handlers run in the .NET Interactive process, this is of no help if you want the command to execute on the client side. So with custom commands intended for client-side handling, you can make this call:

```cs
Kernel.Current.RegisterCommandType<MyToClientCommand>("MyToClientCommand");
```

This makes the command type known to the .NET Interactive kernel's serialization mechanisms, enabling the command to be sent correctly over the communication channel that connects the .NET Interactive process to the notebook host.


## JavaScript API

To enable notebooks to take advantage of kernel-to-client commands, the `interactive` variable made available to JavaScript cells (the shape of which is defined in the `DotnetInteractiveClient` interface in the [dotnet-interactive-interfaces.ts](../src/Microsoft.DotNet.Interactive.Js/src/dotnet-interactive/dotnet-interactive-interfaces.ts) file) defines a `registerCommandHandler` method.

To illustrate its use, suppose we have an HTML cell with the following content:

```html
<div id="output">Init</div>
```

We could then execute this code in a JavaScript cell:

```js
interactive.registerCommandHandler(
    "MyToClientCommand",
    env => {
        let msg = `Message type '${env.commandType}' received: '${env.command.data}'`;
        output.innerText = output.innerText + '\n' + msg;
    });
```

Remember that JavaScript cells do not execute their JavaScript in the main kernel process. The JavaScript kernel essentially reflects the script right back into the client, so this code would run in, for example, the Visual Studio Code script engine. By using the `interactive` object's `registerCommandHandler` method, this code indicates that it wants to handle all commands of type `MyToClientCommand`. In this case, the handler just appends some text to the `<div>` element created by the HTML cell.

**Note**: if a handler was already in place for a particular command type, `registerCommandHandler` replaces that handler. This means that if you run the JavaScript cell that registers a handler multiple times, the handler will _not_ be invoked multiple times.

With this handler in place, we can now write a C# cell that invokes this command.

## .NET API

Code that runs inside the kernel process (typically defined in a code cell, but .NET Interactive extension components can also use these techniques) can send commands to the client-side kernels like this:

```cs
var cmd = new MyToClientCommand("From C# kernel");
var clientKernel = GetKernel("notebook");
await clientKernel.SendAsync(cmd);
```

This obtains a reference to the kernel named `notebook`, which was the name we gave our client-side kernel in the `#!connect` directive earlier. Strictly speaking, `GetKernel` will in this case return a `ProxyKernel` that represents the remote kernel running on the client side, so when we call `SendAsync`, it serializes our command and sends it to the client. The client-side kernel will received this message, deserialize it, and then note that this command's type is `MyToClientCommand`. That is the same type that our script registered a handler for earlier, so it knows to deliver the command to that handler.