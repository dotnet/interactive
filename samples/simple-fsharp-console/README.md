## Simple F\# console

This is the simplest project demonstrating interactive execution of F# commands in runtime.

Build and run:
```
dotnet build
dotnet run FSharpConsole
```

It synchronously waits for your commands and executes them.

These lines
```cs
Formatter.SetPreferredMimeTypeFor(typeof(object), "text/plain");
Formatter.Register<object>(o => o.ToString());
```
allow to print any object with default `.ToString()` method (instead of default html output or custom output for records).

These lines
```cs
var toSubmit = new SubmitCode(request);
var response = await kernel.SendAsync(toSubmit);
```
execute the code from `request`.

Finally, line
```
response.KernelEvents.Subscribe(...)
```
synchronously processes the sequence of events produced by interactive.