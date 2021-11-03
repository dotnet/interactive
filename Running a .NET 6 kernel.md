# Running a .NET 6 kernel of .NET Interactive in VS Code - Insiders

There are 2 ways of running net6.0 .NET Interactive; (1) as a global tool, and (2) as a private tool.  Option (1) is the easiest, but alters the global state of the machine.  Option (2) is slightly more involved, but doesn't change anything globally.

Both options require a copy of the .NET 6 SDK to be installed.

## Option 1 - A globally available .NET Interactive on net6.0

### Install the global tool

Run the following command(s) to install the .NET 6 version of the global tool:

``` bash
# uninstall any old versions
dotnet tool uninstall -g Microsoft.dotnet-interactive

# install the .NET 6 version
dotnet tool install -g Microsoft.dotnet-interactive --version 1.0.250602 --add-source https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json
```

### Set VS Code - Insiders to use the .NET 6 global tool

1. In VS Code - Insiders, type `Ctrl`+`Shift`+`P` to open the command palette.
2. Execute the command "Preferences: Open Settings (JSON)".
3. In the `settings.json` that opens, add the following:

``` javascript
    // ...
    "dotnet-interactive.kernelTransportArgs": [
        "dotnet-interactive",
        "vscode",
        "--working-dir",
        "{working_dir}"
    ],
    // ...
```

4. Save `settings.json` and restart VS Code - Insiders.
5. Any notebooks opened will now be running against the .NET 6 kernel.  To revert back to the defualt behavior, simply comment out/delete the lines from step 3 and restart VS Code - Insiders.

## Option 2 - A private copy of .NET Interactive on net6.0

### Download the .NET 6 version of the tool

0. This part only needs to happen once.
1. Run the following in any .NET Interactive notebook.  It will fail _but_ it dowloaded the .NET 6 version of the tool to the local package cache.

``` csharp
#i "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json"
#r "nuget:Microsoft.dotnet-interactive, 1.0.250602"
```

### Set VS Code - Insiders to use the .NET 6 version of the tool

1. In VS Code - Insiders, type `Ctrl`+`Shift`+`P` to open the command palette.
2. Execute the command "Preferences: Open Settings (JSON)".
3. In the `settings.json` that opens, add the following:

``` javascript
    // ...
    "dotnet-interactive.kernelTransportArgs": [
        "{dotnet_path}",
        "C:/Users/brett/.nuget/packages/microsoft.dotnet-interactive/1.0.250602/tools/net6.0/any/Microsoft.DotNet.Interactive.App.dll",
        //        ^^^^^ this should be updated to match your current user name
        "vscode",
        "--working-dir",
        "{working_dir}"
    ],
    // ...
```

4. Save `settings.json` and restart VS Code - Insiders.
5. Any notebooks opened will now be running against the .NET 6 kernel.  To revert back to the defualt behavior, simply comment out/delete the lines from step 3 and restart VS Code - Insiders.

## Once you're back in VS Code - Insiders, something like the following will now work:

``` csharp
#r "C:\Users\brett\source\repos\MyDotNet6Library\MyDotNet6Library\obj\Debug\net6.0\MyDotNet6Library.dll"
MyDotNet6Library.MyLibrary.AddTwoNumbers(2, 3)
```
