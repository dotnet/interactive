## Download the .NET 6 version of the tool

0. This part only needs to happen once.
1. Run the following in any .NET Interactive notebook.  It will fail _but_ it dowloaded the .NET 6 version of the tool to the local package cache.

``` csharp
#i "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-experimental/nuget/v3/index.json"
#r "nuget:Microsoft.dotnet-interactive, 1.0.250102"
```

## Set VS Code - Insiders to use the .NET 6 version of the tool

1. In VS Code - Insiders, type `Ctrl`+`Shift`+`P` to open the command palette.
2. Execute the command "Preferences: Open Settings (JSON)".
3. In the `settings.json` that opens, add the following:

``` javascript
    // ...
    "dotnet-interactive.kernelTransportArgs": [
        "{dotnet_path}",
        "C:/Users/brett/.nuget/packages/microsoft.dotnet-interactive/1.0.250102/tools/net6.0/any/Microsoft.DotNet.Interactive.App.dll",
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
